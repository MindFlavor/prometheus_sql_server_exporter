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

        public override string ToString()
        {
            return $"{this.GetType().Name}[type == {type}, name == {name}]";
        }
    }

    public class PerformanceCounters
    {
        private static HashSet<string>? EnabledCounters { get; set; } = null;
        static Dictionary<string, GrafanaPerformanceCounter> _dGraf;

        public SQLServerInfo SQLServerInfo;
        private ILogger<PerformanceCounters> logger;

        static PerformanceCounters()
        {
            _dGraf = new Dictionary<string, GrafanaPerformanceCounter>();

            using (System.IO.StreamReader sr = new System.IO.StreamReader(System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("MindFlavor.SQLServerExporter.embed.PerformanceCountersMapping.csv")!))
            {
                string? s;
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

        public PerformanceCounters(HttpContext context, SQLServerInfo sqlServerInfo)
        {
            this.SQLServerInfo = sqlServerInfo;
            logger = context.RequestServices.GetRequiredService<ILogger<PerformanceCounters>>();

            // in theory we should guard this code against concurrent access. It is possibile that
            // two or more threads will work on the same HashSet concurrently because the all
            // tested Waits == null to be true. In practice this will never happen as long as it's only
            // one Prometheus calling our method. For now we will avoid the cost of a mutex, if bugs
            // arise it will be included here.
            if (EnabledCounters == null)
            {
                EnabledCounters = new HashSet<string>();

                foreach (var file in Program.CommandLineOptions!.PerformanceCounters.TemplateFiles)
                {
                    System.IO.FileInfo fi = new System.IO.FileInfo(file);
                    logger.LogInformation($"Loading performance counter template to include from {fi.FullName}...");

                    using (System.IO.StreamReader sr = new System.IO.StreamReader(new System.IO.FileStream(fi.FullName,
                    System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.Read)))
                    {
                        string? str;
                        while ((str = sr.ReadLine()) != null)
                        {
                            if (str.StartsWith("#"))
                                continue;

                            EnabledCounters.Add(str);
                        }
                    }
                }
            }
        }

        private static string KeyFromObjectNameAndCounterName(string objectName, string counterName)
        {
            return objectName + "_" + counterName;
        }

        public string QueryAndSerializeData()
        {
            if (EnabledCounters == null)
                throw new Exception("EnabledCounters must not be null at this phase.");
            else
            {
                using (SqlConnection conn = new SqlConnection(this.SQLServerInfo.ConnectionString))
                {
                    logger.LogDebug($"About to open connection to {this.SQLServerInfo.Name}");
                    conn.Open();

                    System.Text.StringBuilder sb = new System.Text.StringBuilder();

                    string tsql = TSQLStore.ProbeTSQL("performance_counters", this.SQLServerInfo);

                    logger.LogDebug($"Probing performance counters for {this.SQLServerInfo.Name}, version {this.SQLServerInfo.Version} returned {tsql}");

                    using (SqlCommand cmd = new SqlCommand(tsql, conn))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string objectName = reader.GetString(0).Trim();
                                // this will strip the prefix because is dependent on the instance
                                // name (like MSSQL$SQL17:Locks, MSSQL$SQL17:Databases etc...)
                                // and we will store the info in the instance attribute instead
                                int idx = objectName.IndexOf(":");
                                if (idx != -1)
                                    objectName = objectName.Substring(idx + 1);

                                string counterName = reader.GetString(1).Trim();
                                string? instanceName = reader.IsDBNull(2) ? null : reader.GetString(2).Trim();
                                long cntr_value = reader.GetInt64(3);

                                string key = KeyFromObjectNameAndCounterName(objectName, counterName);

                                GrafanaPerformanceCounter gpc;
                                if (_dGraf.TryGetValue(key, out gpc))
                                {
                                    // skip this is it's not in the enabled performance counters
                                    // as specified by the template files configured.
                                    if (!EnabledCounters.Contains(gpc.name))
                                    {
                                        logger.LogDebug($"PerformanceCounter {gpc} will be skipped because it's not in any configured template file");
                                        continue;
                                    }

                                    string gpcName = $"sql_pc_{gpc.name}";

                                    //sb.Append($"# TYPE {gpcName} {gpc.type}\n");

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
}
