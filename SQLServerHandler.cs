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
using MindFlavor.Prometheus;

namespace MindFlavor.SQLServerExporter
{
    public class SQLServerHandler
    {
        public static void Handler(IApplicationBuilder app)
        {
            app.Run(async context =>
            {
                var logger = context.RequestServices.GetRequiredService<ILogger<SQLServerHandler>>();
                logger.LogDebug($"Called method {context.Request.Method}, remote IP {context.Connection?.RemoteIpAddress?.ToString()}");
                if (context.Request.Method != "GET")
                {
                    context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
                    return;
                }

                List<Thread> lThreads = new List<Thread>();
                ConcurrentMetricDictionary sharedMetricDictionary = new ConcurrentMetricDictionary();

                logger.LogDebug($"Before foreach(... {Program.CommandLineOptions!.Instances.Count})");
                try
                {
                    foreach (var instance in Program.CommandLineOptions.Instances)
                    {
                        logger.LogDebug($"Adding ProcessInstance(context == {context.ToString()}, instance == {instance.ConnectionString}, sharedMetricDictionary = <..>)");

                        var pts = new ParameterizedThreadStart(ProcessInstance);
                        Thread thread = new Thread(pts);
                        thread.Priority = ThreadPriority.BelowNormal;
                        thread.Name = new System.Data.SqlClient.SqlConnectionStringBuilder(instance.ConnectionString).DataSource;
                        thread.Start(new Tuple<HttpContext, string, ConcurrentMetricDictionary>(context, instance.ConnectionString, sharedMetricDictionary));
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

                    lThreads.ForEach(t => t.Join());

                    context.Response.StatusCode = StatusCodes.Status200OK;
                    await context.Response.WriteAsync(sharedMetricDictionary.Render(false));
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
            Tuple<HttpContext, string, ConcurrentMetricDictionary> tuple = (Tuple<HttpContext, string, ConcurrentMetricDictionary>)o!;
            HttpContext context = tuple.Item1;
            string connectionString = tuple.Item2;
            ConcurrentMetricDictionary sharedMetricDictionary = tuple.Item3;

            var logger = context.RequestServices.GetRequiredService<ILogger<SQLServerHandler>>();
            logger.LogTrace($"Called ProcessInstance(connectionString = {connectionString}, ...)");

            try
            {
                var sqlServerInfo = SQLServerUtils.GetSQLServerInfo(context, connectionString);
                new Counters.WorkerThread(context, sqlServerInfo).QueryAndAddToSharedMetricDictionary(sharedMetricDictionary);
                new Counters.PerformanceCounters(context, sqlServerInfo).QueryAndAddToSharedMetricDictionary(sharedMetricDictionary);
                new Counters.WaitStats(context, sqlServerInfo).QueryAndAddToSharedMetricDictionary(sharedMetricDictionary);
                new Counters.MemoryClerks(context, sqlServerInfo).QueryAndAddToSharedMetricDictionary(sharedMetricDictionary);

                if (Program.CommandLineOptions != null)
                {
                    foreach (var customCounterConfiguration in Program.CommandLineOptions.CustomCounters)
                    {
                        new Counters.CustomCounter(context, sqlServerInfo, customCounterConfiguration.CustomCounter).QueryAndAddToSharedMetricDictionary(sharedMetricDictionary);
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
