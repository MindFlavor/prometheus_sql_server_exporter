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

        private static HashSet<string>? Waits { get; set; }
        private static string TSQLQuery { get; set; } = string.Empty;

        public WaitStats(HttpContext context, SQLServerInfo sqlServerInfo)
        {
            this.SQLServerInfo = sqlServerInfo;
            logger = context.RequestServices.GetRequiredService<ILogger<WaitStats>>();

            // in theory we should guard this code against concurrent access. It is possibile that
            // two or more threads will work on the same HashSet concurrently because the all
            // tested Waits == null to be true. In practice this will never happen as long as it's only
            // one Prometheus calling our method. For now we will avoid the cost of a mutex, if bugs
            // arise it will be included here.
            if (Waits == null)
            {
                Waits = new HashSet<string>();

                foreach (var file in Program.CommandLineOptions!.WaitStats.TemplateFiles)
                {
                    System.IO.FileInfo fi = new System.IO.FileInfo(file);
                    logger.LogInformation($"Loading wait stats template to include from {fi.FullName}...");

                    using (System.IO.StreamReader sr = new System.IO.StreamReader(new System.IO.FileStream(fi.FullName,
                    System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.Read)))
                    {
                        string? str;
                        while ((str = sr.ReadLine()) != null)
                        {
                            if (str.StartsWith("#"))
                                continue;

                            Waits.Add(str);
                        }
                    }
                }

                string tsql = TSQLStore.ProbeTSQL("wait_stats", this.SQLServerInfo);
                logger.LogDebug($"Probing wait statistics for {this.SQLServerInfo.Name}, version {this.SQLServerInfo.Version} returned {tsql}");

                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                bool fFirst = true;
                foreach (string str in Waits)
                {
                    if (!fFirst)
                        sb.Append(",\n");
                    else
                        fFirst = false;
                    sb.Append($"  N'{str}'");
                }

                TSQLQuery = tsql.Replace("%%WAITS%%", sb.ToString());
            }
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
                            string waitType = reader.GetString(0);
                            long waitingTasksCount = reader.GetInt64(1);
                            long waitTimeMS = reader.GetInt64(2);

                            dict.Add("sql_waiting_tasks_count", "gauge", "sql_waiting_tasks_count", $"sql_waiting_tasks_count{{instance=\"{this.SQLServerInfo.Name}\", wait=\"{waitType}\"}} {waitingTasksCount}");
                            dict.Add("sql_wait_time_ms", "counter", "sql_wait_time_ms", $"sql_wait_time_ms{{instance=\"{this.SQLServerInfo.Name}\", wait=\"{waitType}\"}} {waitTimeMS}");
                        }
                    }
                }

                return dict;
            }
        }
    }
}
