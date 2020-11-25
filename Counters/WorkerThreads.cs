using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Data.SqlClient;
using Microsoft.AspNetCore.Http;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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

                System.Text.StringBuilder sb = new System.Text.StringBuilder();

                string tsql = TSQLStore.ProbeTSQL("worker_threads", this.SQLServerInfo);

                logger.LogDebug($"Probing worker_threads for {this.SQLServerInfo.Name}, version {this.SQLServerInfo.Version} returned {tsql}");

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
                                sb.Append($"sql_os_schedulers_{reader.GetName(i)}{{instance=\"{this.SQLServerInfo.Name}\", parent_node_id=\"{parent_node_id}\", scheduler_id=\"{scheduler_id}\", cpu_id=\"{cpu_id}\"}} ");
                                if (reader.GetFieldType(i) == typeof(bool))
                                    sb.Append($"{(reader.GetBoolean(i) == true ? "1" : "0")}\n");
                                else if (reader.GetFieldType(i) == typeof(Int32))
                                    sb.Append($"{reader.GetInt32(i)}\n");
                                else
                                    sb.Append($"{reader.GetInt64(i)}\n");
                            }
                        }
                    }
                }

                return sb.ToString();
            }
        }

    }
}