using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MindFlavor.Prometheus;

namespace MindFlavor.SQLServerExporter.Counters
{
    public class WorkerThread
    {
        public SQLServerInfo SQLServerInfo;
        private ILogger<WorkerThread> logger;

        public WorkerThread(HttpContext context, SQLServerInfo sqlServerInfo)
        {
            this.SQLServerInfo = sqlServerInfo;
            logger = context.RequestServices.GetRequiredService<ILogger<WorkerThread>>();
        }

        public void QueryAndAddToSharedMetricDictionary(ConcurrentMetricDictionary sharedMetricDictionary)
        {
            Metric[]? metrics = null;

            using (SqlConnection conn = new SqlConnection(this.SQLServerInfo.ConnectionString))
            {
                logger.LogDebug($"About to open connection to {this.SQLServerInfo.Name}");
                conn.Open();

                string tsql = TSQLStore.ProbeTSQL("worker_threads", this.SQLServerInfo);
                logger.LogDebug($"Probing worker_threads for {this.SQLServerInfo.Name}, version {this.SQLServerInfo.Version} returned {tsql}");

                using (SqlCommand cmd = new SqlCommand(tsql, conn))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        // create 1 metric for each column (except the first 3)
                        const int COLUMNS_TO_SKIP = 3;

                        metrics = new Metric[reader.FieldCount - COLUMNS_TO_SKIP];
                        for (int i = 0; i < metrics.Length; i++)
                        {
                            var metricName = $"sql_os_scheduler_{reader.GetName(i + COLUMNS_TO_SKIP)}";
                            metrics[i] = new Metric(metricName, metricName, "gauge");
                        }

                        while (reader.Read())
                        {
                            int parent_node_id = reader.GetInt32(0);
                            int scheduler_id = reader.GetInt32(1);
                            int cpu_id = reader.GetInt32(2);

                            for (int i = 0; i < metrics.Length; i++)
                            {
                                var instance = new Prometheus.Instance(this.SQLServerInfo.Name);
                                instance.Attributes.Add(new KeyValuePair<string, string>("parent_node_id", parent_node_id.ToString()));
                                instance.Attributes.Add(new KeyValuePair<string, string>("scheduler_id", scheduler_id.ToString()));
                                instance.Attributes.Add(new KeyValuePair<string, string>("cpu_id", cpu_id.ToString()));

                                if (reader.GetFieldType(i + COLUMNS_TO_SKIP) == typeof(bool))
                                    instance.Value = reader.GetBoolean(i + COLUMNS_TO_SKIP) == true ? "1" : "0";
                                else if (reader.GetFieldType(i + COLUMNS_TO_SKIP) == typeof(Int32))
                                    instance.Value = reader.GetInt32(i + COLUMNS_TO_SKIP).ToString();
                                else
                                    instance.Value = reader.GetInt64(i + COLUMNS_TO_SKIP).ToString();

                                metrics[i].Instances.Add(instance);
                            }
                        }
                    }
                }

                sharedMetricDictionary.Merge(metrics);
            }
        }

    }
}