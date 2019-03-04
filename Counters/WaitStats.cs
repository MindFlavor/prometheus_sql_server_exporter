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
    public class WaitStats
    {
        public SQLServerInfo SQLServerInfo;
        private ILogger<WaitStats> logger;

        public WaitStats(HttpContext context, SQLServerInfo sqlServerInfo)
        {
            this.SQLServerInfo = sqlServerInfo;
            logger = context.RequestServices.GetRequiredService<ILogger<WaitStats>>();
        }

        public async Task<string> QueryAndSerializeData()
        {
            using (SqlConnection conn = new SqlConnection(this.SQLServerInfo.ConnectionString))
            {
                logger.LogDebug($"About to open connection to {this.SQLServerInfo.Name}");
                await conn.OpenAsync();

                System.Text.StringBuilder sbTasksCount = new System.Text.StringBuilder();
                System.Text.StringBuilder sbWaitTimeMS = new System.Text.StringBuilder();

                string tsql = TSQLStore.ProbeTSQL("wait_stats", this.SQLServerInfo);

                logger.LogDebug($"Probing wait statistics for {this.SQLServerInfo.Name}, version {this.SQLServerInfo.Version} returned {tsql}");

                using (SqlCommand cmd = new SqlCommand(tsql, conn))
                {
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            string waitType = reader.GetString(0);
                            long waitingTasksCount = reader.GetInt64(1);
                            long waitTimeMS = reader.GetInt64(2);

                            sbTasksCount.Append($"sql_waiting_tasks_count{{instance=\"{this.SQLServerInfo.Name}\", wait=\"{waitType}\"}} {waitingTasksCount}\n");
                            sbWaitTimeMS.Append($"sql_wait_time_ms{{instance=\"{this.SQLServerInfo.Name}\", wait=\"{waitType}\"}} {waitTimeMS}\n");
                        }
                    }
                }

                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                sb.Append("# TYPE sql_waiting_tasks_count gauge\n");
                sb.Append(sbTasksCount);

                sb.Append("# TYPE sql_wait_time_ms counter\n");
                sb.Append(sbWaitTimeMS);

                return sb.ToString();
            }
        }
    }
}
