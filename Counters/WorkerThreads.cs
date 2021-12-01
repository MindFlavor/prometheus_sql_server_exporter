using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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

        public string QueryAndSerializeData()
        {
            using (SqlConnection conn = new SqlConnection(this.SQLServerInfo.ConnectionString))
            {
                logger.LogDebug($"About to open connection to {this.SQLServerInfo.Name}");
                conn.Open();

                string tsql = TSQLStore.ProbeTSQL("worker_threads", this.SQLServerInfo);

                logger.LogDebug($"Probing worker_threads for {this.SQLServerInfo.Name}, version {this.SQLServerInfo.Version} returned {tsql}");


                var metric = new Prometheus.Metric("sql_os_scheduler", "sql_os_scheduler", "gauge");

                using (SqlCommand cmd = new SqlCommand(tsql, conn))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int parent_node_id = reader.GetInt32(0);
                            int scheduler_id = reader.GetInt32(1);
                            int cpu_id = reader.GetInt32(2);

                            for (int i = 3; i < reader.FieldCount; i++)
                            {
                                var instance = new Prometheus.Instance(this.SQLServerInfo.Name);
                                instance.Attributes.Add(new KeyValuePair<string, string>("parent_node_id", parent_node_id.ToString()));
                                instance.Attributes.Add(new KeyValuePair<string, string>("scheduler_id", scheduler_id.ToString()));
                                instance.Attributes.Add(new KeyValuePair<string, string>("cpu_id", cpu_id.ToString()));

                                if (reader.GetFieldType(i) == typeof(bool))
                                    instance.Value = reader.GetBoolean(i) == true ? "1" : "0";
                                else if (reader.GetFieldType(i) == typeof(Int32))
                                    instance.Value = reader.GetInt32(i).ToString();
                                else
                                    instance.Value = reader.GetInt64(i).ToString();
                            }
                        }
                    }
                }

                return metric.Render();
            }
        }

    }
}