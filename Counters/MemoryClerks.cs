using System.Collections.Generic;
using System.Threading.Tasks;
using System.Data.SqlClient;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MindFlavor.Prometheus;

namespace MindFlavor.SQLServerExporter.Counters
{
    public class MemoryClerks
    {
        public SQLServerInfo SQLServerInfo;
        private ILogger<MemoryClerks> logger;

        private static HashSet<string>? Waits { get; set; }
        private static string TSQLQuery { get; set; } = string.Empty;

        public MemoryClerks(HttpContext context, SQLServerInfo sqlServerInfo)
        {
            this.SQLServerInfo = sqlServerInfo;
            logger = context.RequestServices.GetRequiredService<ILogger<MemoryClerks>>();

            TSQLQuery = TSQLStore.ProbeTSQL("memory_clerks", this.SQLServerInfo);
        }

        public void QueryAndAddToSharedMetricDictionary(ConcurrentMetricDictionary sharedMetricDictionary)
        {
            using (SqlConnection conn = new SqlConnection(this.SQLServerInfo.ConnectionString))
            {
                logger.LogDebug($"About to open connection to {this.SQLServerInfo.Name}");
                conn.Open();

                var pSumPagesKB = new Prometheus.Metric("sql_memory_clerks_sum_pages_kb", "sql_memory_clerks_sum_pages_kb", "gauge");
                var pSumVirtualMemoryReservedKB = new Prometheus.Metric("sql_memory_clerks_sum_virtual_memory_reserved_kb", "sql_memory_clerks_sum_virtual_memory_reserved_kb", "gauge");
                var pSumVirtualMemoryCommittedKB = new Prometheus.Metric("sql_memory_clerks_sum_virtual_memory_committed_kb", "sql_memory_clerks_sum_virtual_memory_committed_kb", "gauge");
                var pSumSharedMemoryReservedKB = new Prometheus.Metric("sql_memory_clerks_sum_shared_memory_reserved_kb", "sql_memory_clerks_sum_shared_memory_reserved_kb", "gauge");
                var pSumSharedMemoryCommittedKB = new Prometheus.Metric("sql_memory_clerks_sum_shared_memory_committed_kb", "sql_memory_clerks_sum_shared_memory_committed_kb", "gauge");

                using (SqlCommand cmd = new SqlCommand(TSQLQuery, conn))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string clerk_name = reader.GetString(0);
                            long sum_pages_kb = reader.GetInt64(1);
                            long sum_virtual_memory_reserved_kb = reader.GetInt64(2);
                            long sum_virtual_memory_committed_kb = reader.GetInt64(3);
                            long sum_shared_memory_reserved_kb = reader.GetInt64(4);
                            long sum_shared_memory_committed_kb = reader.GetInt64(5);

                            Prometheus.Instance instance = new Prometheus.Instance(this.SQLServerInfo.Name);
                            instance.Attributes.Add(new KeyValuePair<string, string>("clerk", clerk_name));
                            instance.Value = sum_pages_kb.ToString();
                            pSumPagesKB.Instances.Add(instance);

                            instance = new Prometheus.Instance(this.SQLServerInfo.Name);
                            instance.Attributes.Add(new KeyValuePair<string, string>("clerk", clerk_name));
                            instance.Value = sum_virtual_memory_reserved_kb.ToString();
                            pSumSharedMemoryReservedKB.Instances.Add(instance);

                            instance = new Prometheus.Instance(this.SQLServerInfo.Name);
                            instance.Attributes.Add(new KeyValuePair<string, string>("clerk", clerk_name));
                            instance.Value = sum_virtual_memory_committed_kb.ToString();
                            pSumVirtualMemoryCommittedKB.Instances.Add(instance);

                            instance = new Prometheus.Instance(this.SQLServerInfo.Name);
                            instance.Attributes.Add(new KeyValuePair<string, string>("clerk", clerk_name));
                            instance.Value = sum_shared_memory_reserved_kb.ToString();
                            pSumSharedMemoryReservedKB.Instances.Add(instance);

                            instance = new Prometheus.Instance(this.SQLServerInfo.Name);
                            instance.Attributes.Add(new KeyValuePair<string, string>("clerk", clerk_name));
                            instance.Value = sum_shared_memory_committed_kb.ToString();
                            pSumSharedMemoryCommittedKB.Instances.Add(instance);
                        }
                    }
                }

                sharedMetricDictionary.Merge(new Metric[] {
                    pSumPagesKB,
                    pSumVirtualMemoryReservedKB,
                    pSumVirtualMemoryCommittedKB,
                    pSumSharedMemoryReservedKB,
                    pSumSharedMemoryCommittedKB
                });

            }
        }
    }
}
