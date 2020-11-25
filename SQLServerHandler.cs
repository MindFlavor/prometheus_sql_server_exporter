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
                ConcurrentBag<string> bag = new ConcurrentBag<string>();
                logger.LogDebug($"Before foreach(... {Program.CommandLineOptions!.Instances.Count})");
                try
                {
                    foreach (var instance in Program.CommandLineOptions.Instances)
                    {
                        logger.LogDebug($"Adding ProcessInstance({context.ToString()}, {instance.ConnectionString}, {bag.ToString()}");

                        var pts = new ParameterizedThreadStart(ProcessInstance);
                        Thread thread = new Thread(pts);
                        thread.Priority = ThreadPriority.BelowNormal;
                        thread.Name = instance.ConnectionString;
                        thread.Start(new Tuple<HttpContext, string, ConcurrentBag<string>>(context, instance.ConnectionString, bag));
                        lThreads.Add(thread);
                    }

                    DateTime dtStartTimeout = DateTime.Now;
                    while ((DateTime.Now - dtStartTimeout).TotalSeconds < Program.CommandLineOptions.InstanceTotalTimeout)
                    {
                        lThreads.ForEach(t => t.Join(100));
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
                    foreach (var s in bag)
                    {
                        sb.Append(s);
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
            Tuple<HttpContext, string, ConcurrentBag<string>> tuple = (Tuple<HttpContext, string, ConcurrentBag<string>>)o!;
            HttpContext context = tuple.Item1;
            string connectionString = tuple.Item2;
            ConcurrentBag<string> bag = tuple.Item3;

            var logger = context.RequestServices.GetRequiredService<ILogger<SQLServerHandler>>();
            logger.LogTrace($"Called ProcessInstance(connectionString = {connectionString}, bag={bag.ToString()}");

            try
            {
                var sqlServerInfo = SQLServerUtils.GetSQLServerInfo(context, connectionString);
                var retString = new Counters.WorkerThread(context, sqlServerInfo).QueryAndSerializeData();
                bag.Add(retString);

                var pc = new Counters.PerformanceCounters(context, sqlServerInfo).QueryAndSerializeData();
                bag.Add(pc);

                var waitStats = new Counters.WaitStats(context, sqlServerInfo).QueryAndSerializeData();
                bag.Add(waitStats);

                var clerks = new Counters.MemoryClerks(context, sqlServerInfo).QueryAndSerializeData();
                bag.Add(clerks);

                if (Program.CommandLineOptions != null)
                {
                    foreach (var customCounterConfiguration in Program.CommandLineOptions.CustomCounters)
                    {
                        bag.Add(
                            new Counters.CustomCounter(context, sqlServerInfo, customCounterConfiguration.CustomCounter).QueryAndSerializeData()
                        );
                    }
                }
            }
            catch (Exception e)
            {
                logger.LogWarning($"unhandled exception: {e.ToString()}");
            }
        }
    }
}
