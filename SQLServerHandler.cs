using System;
using System.Threading;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Http;

namespace MindFlavor.SQLServerExporter
{
    public class SQLServerHandler
    {
        public static void Handler(IApplicationBuilder app)
        {
            app.Run(async context =>
            {
                var logger = context.RequestServices.GetRequiredService<ILogger<SQLServerHandler>>();
                logger.LogDebug($"Called method {context.Request.Method}, remote IP {context.Connection.RemoteIpAddress.ToString()}");
                if (context.Request.Method != "GET")
                {
                    context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
                    return;
                }

                List<Thread> lThreads = new List<Thread>();
                ConcurrentDictionary<string, ConcurrentBag<string>> dictBag = new ConcurrentDictionary<string, ConcurrentBag<string>>();

                logger.LogDebug($"Before foreach(... {Program.CommandLineOptions!.Instances.Count})");
                try
                {
                    foreach (var instance in Program.CommandLineOptions.Instances)
                    {
                        logger.LogDebug($"Adding ProcessInstance({context.ToString()}, {instance.ConnectionString}, {dictBag.ToString()}");

                        var pts = new ParameterizedThreadStart(ProcessInstance);
                        Thread thread = new Thread(pts);
                        thread.Priority = ThreadPriority.BelowNormal;
                        thread.Name = instance.ConnectionString;
                        thread.Start(new Tuple<HttpContext, string,
                            System.Collections.Concurrent.ConcurrentDictionary<string, ConcurrentBag<string>>>
                             (context, instance.ConnectionString, dictBag));
                        lThreads.Add(thread);
                    }

                    DateTime dtStartTimeout = DateTime.Now;
                    while ((DateTime.Now - dtStartTimeout).TotalSeconds < Program.CommandLineOptions.InstanceTotalTimeout)
                    {
                        if (lThreads.All(t => t.Join(100)))
                            break;
                    }

                    // if we arrive here and some threads are still
                    // running it means we are over the allotted
                    // time so we kill the thread. We might
                    // not have all the data.
                    lThreads.ForEach(t =>
                    {
                        if (!t.Join(0))
                        {
                            logger.LogWarning($"Killing thread [{t.ManagedThreadId}] {t.Name} because it took more than the configured time ({Program.CommandLineOptions.InstanceTotalTimeout} secs)");
                            t.Interrupt();
                        }
                    });

                    context.Response.StatusCode = StatusCodes.Status200OK;

                    System.Text.StringBuilder sb = new System.Text.StringBuilder();
                    foreach (var bags in dictBag.Values)
                    {
                        foreach (var s in bags)
                        {
                            sb.Append(s);
                        }
                    }

                    await context.Response.WriteAsync(sb.ToString());
                }
                catch (Exception exce)
                {
                    logger.LogError(exce.Message);
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                }
            });
        }

        private static void ProcessInstance(object? o)
        {
            Tuple<HttpContext, string, ConcurrentDictionary<string, ConcurrentBag<string>>> tuple = (Tuple<HttpContext, string, ConcurrentDictionary<string, ConcurrentBag<string>>>)o!;
            HttpContext context = tuple.Item1;
            string connectionString = tuple.Item2;
            ConcurrentDictionary<string, ConcurrentBag<string>> dictBags = tuple.Item3;

            var logger = context.RequestServices.GetRequiredService<ILogger<SQLServerHandler>>();
            logger.LogTrace($"Called ProcessInstance(connectionString = {connectionString}, dictBags={dictBags.ToString()}");

            try
            {
                var sqlServerInfo = SQLServerUtils.GetSQLServerInfo(context, connectionString);
                var retString = new Counters.WorkerThread(context, sqlServerInfo).QueryAndSerializeData();
                dictBags.GetOrAdd("worker_thread", new ConcurrentBag<string>()).Add(retString);

                var pc = new Counters.PerformanceCounters(context, sqlServerInfo).QueryAndSerializeData();
                dictBags.GetOrAdd("performance_counters", new ConcurrentBag<string>()).Add(pc);

                var waitStats = new Counters.WaitStats(context, sqlServerInfo).QueryAndSerializeData();
                dictBags.GetOrAdd("wait_stats", new ConcurrentBag<string>()).Add(waitStats);

                var clerks = new Counters.MemoryClerks(context, sqlServerInfo).QueryAndSerializeData();
                dictBags.GetOrAdd("memory_clerks", new ConcurrentBag<string>()).Add(clerks);

                if (Program.CommandLineOptions != null)
                {
                    foreach (var customCounterConfiguration in Program.CommandLineOptions.CustomCounters)
                    {
                        var l =
                            new Counters.CustomCounter(context, sqlServerInfo, customCounterConfiguration.CustomCounter).QueryAndSerializeData();
                        dictBags.GetOrAdd(customCounterConfiguration.CustomCounter.Name, new ConcurrentBag<string>()).Add(l);
                    }
                }
            }
            catch (Exception e)
            {
                try
                {
                    System.Data.SqlClient.SqlConnectionStringBuilder connBuilder = new System.Data.SqlClient.SqlConnectionStringBuilder(connectionString);
                    logger.LogWarning($"unhandled exception processing \"{connBuilder.DataSource}\": {e.ToString()}");
                }
                catch (Exception einner)
                {
                    logger.LogWarning($"unhandled exception: {einner.ToString()}");
                }
            }
        }
    }
}
