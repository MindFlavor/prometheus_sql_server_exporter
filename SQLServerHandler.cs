using System;
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

                List<Task> lTasks = new List<Task>();
                ConcurrentBag<string> bag = new ConcurrentBag<string>();
                logger.LogDebug($"Before foreach(... {Program.CommandLineOptions.Instances.Count})");
                try
                {
                    foreach (var instance in Program.CommandLineOptions.Instances)
                    {
                        logger.LogDebug($"Adding ProcessInstance({context.ToString()}, {instance.ConnectionString}, {bag.ToString()}");
                        lTasks.Add(ProcessInstance(context, instance.ConnectionString, bag));
                    }

                    await Task.WhenAll(lTasks.ToArray());

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

        private static async Task ProcessInstance(HttpContext context, string connectionString, ConcurrentBag<string> bag)
        {
            var logger = context.RequestServices.GetRequiredService<ILogger<SQLServerHandler>>();
            logger.LogTrace($"Called ProcessInstance(connectionString = {connectionString}, bag={bag.ToString()}");

            var sqlServerInfo = await SQLServerUtils.GetSQLServerInfo(context, connectionString);
            var retString = await new Counters.WorkerThread(context, sqlServerInfo).QueryAndSerializeData();
            bag.Add(retString);
            var pc = await new Counters.PerformanceCounters(context, sqlServerInfo).QueryAndSerializeData();
            bag.Add(pc);
        }
    }
}
