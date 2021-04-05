using System.Collections.Generic;
using System.Threading.Tasks;
using System.Data.SqlClient;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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

        public PrometheusInstanceDictionary QueryAndSerializeData()
        {
            using (SqlConnection conn = new SqlConnection(this.SQLServerInfo.ConnectionString))
            {
                logger.LogDebug($"About to open connection to {this.SQLServerInfo.Name}");
                conn.Open();

                PrometheusInstanceDictionary dict = new PrometheusInstanceDictionary();

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

                            dict.Add("sql_memory_clerks_sum_pages_kb", "gauge", "sql_memory_clerks_sum_pages_kb", $"sql_memory_clerks_sum_pages_kb{{instance=\"{this.SQLServerInfo.Name}\", clerk=\"{clerk_name}\"}} {sum_pages_kb}");
                            dict.Add("sql_memory_clerks_sum_virtual_memory_reserved_kb", "gauge", "sql_memory_clerks_sum_virtual_memory_reserved_kb", $"sql_memory_clerks_sum_virtual_memory_reserved_kb{{instance=\"{this.SQLServerInfo.Name}\", clerk=\"{clerk_name}\"}} {sum_virtual_memory_reserved_kb}");
                            dict.Add("sql_memory_clerks_sum_virtual_memory_committed_kb", "gauge", "sql_memory_clerks_sum_virtual_memory_committed_kb", $"sql_memory_clerks_sum_virtual_memory_committed_kb{{instance=\"{this.SQLServerInfo.Name}\", clerk=\"{clerk_name}\"}} {sum_virtual_memory_committed_kb}");
                            dict.Add("sql_memory_clerks_sum_shared_memory_reserved_kb", "gauge", "sql_memory_clerks_sum_shared_memory_reserved_kb", $"sql_memory_clerks_sum_shared_memory_reserved_kb{{instance=\"{this.SQLServerInfo.Name}\", clerk=\"{clerk_name}\"}} {sum_shared_memory_reserved_kb}");
                            dict.Add("sql_memory_clerks_sum_shared_memory_committed_kb", "gauge", "sql_memory_clerks_sum_shared_memory_committed_kb", $"sql_memory_clerks_sum_shared_memory_committed_kb{{instance=\"{this.SQLServerInfo.Name}\", clerk=\"{clerk_name}\"}} {sum_shared_memory_committed_kb}");
                        }
                    }
                }

                return dict;
            }
        }
    }
}
