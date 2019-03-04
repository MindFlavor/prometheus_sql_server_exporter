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
    public struct GrafanaPerformanceCounter
    {
        public string type;
        public string name;
    }

    public class PerformanceCounters
    {
        static Dictionary<string, GrafanaPerformanceCounter> _dGraf;

        public SQLServerInfo SQLServerInfo;
        private ILogger<PerformanceCounters> logger;

        static PerformanceCounters()
        {
            _dGraf = new Dictionary<string, GrafanaPerformanceCounter>();
            using (System.IO.StreamReader sr = new System.IO.StreamReader(System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("MindFlavor.SQLServerExporter.embed.PerformanceCountersMapping.csv")))
            {
                string s;
                while ((s = sr.ReadLine()) != null)
                {
                    string[] toks = s.Split(',');
                    string key = KeyFromObjectNameAndCounterName(toks[0], toks[1]);
                    if (_dGraf.ContainsKey(key))
                    {
                        Console.WriteLine($"Key {key} duplicated, ignoring {s}");
                    }
                    else
                    {
                        _dGraf[key] = new GrafanaPerformanceCounter() { type = toks[2], name = toks[3] };
                    }
                }
            }
        }

        private static string KeyFromObjectNameAndCounterName(string objectName, string counterName)
        {
            return objectName + "_" + counterName;
        }

        public PerformanceCounters(HttpContext context, SQLServerInfo sqlServerInfo)
        {
            this.SQLServerInfo = sqlServerInfo;
            logger = context.RequestServices.GetRequiredService<ILogger<PerformanceCounters>>();
        }

        public async Task<string> QueryAndSerializeData()
        {
            using (SqlConnection conn = new SqlConnection(this.SQLServerInfo.ConnectionString))
            {
                logger.LogDebug($"About to open connection to {this.SQLServerInfo.Name}");
                await conn.OpenAsync();

                System.Text.StringBuilder sb = new System.Text.StringBuilder();

                string tsql = TSQLStore.ProbeTSQL("performance_counters", this.SQLServerInfo);

                logger.LogDebug($"Probing performance counters for {this.SQLServerInfo.Name}, version {this.SQLServerInfo.Version} returned {tsql}");

                using (SqlCommand cmd = new SqlCommand(tsql, conn))
                {
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            string objectName = reader.GetString(0).Trim();
                            // this will strip the prefix because is dependent on the instance
                            // name (like MSSQL$SQL17:Locks, MSSQL$SQL17:Databases etc...)
                            // and we will store the info in the instance attribute instead
                            int idx = objectName.IndexOf(":");
                            if (idx != -1)
                                objectName = objectName.Substring(idx + 1);

                            string counterName = reader.GetString(1).Trim();
                            string instanceName = reader.IsDBNull(2) ? null : reader.GetString(2).Trim();
                            long cntr_value = reader.GetInt64(3);

                            string key = KeyFromObjectNameAndCounterName(objectName, counterName);

                            GrafanaPerformanceCounter gpc;
                            if (_dGraf.TryGetValue(key, out gpc))
                            {
                                string gpcName = $"sql_pc_{gpc.name}";

                                sb.Append($"# TYPE {gpcName} {gpc.type}\n");

                                string completeName = $"{gpcName}{{instance=\"{this.SQLServerInfo.Name}\"";

                                if (!string.IsNullOrEmpty(instanceName))
                                {
                                    completeName += $", counter_instance=\"{instanceName}\"";
                                }
                                completeName += "}";

                                sb.Append($"{completeName} {cntr_value.ToString()}\n");
                            }
                            else
                            {
                                logger.LogWarning($"entry {key} not mapped in the mapping file! Ignored in the output");
                            }
                        }
                    }
                }

                return sb.ToString();
            }
        }
    }
}
