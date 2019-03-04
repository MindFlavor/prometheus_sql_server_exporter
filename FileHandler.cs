using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.IO;

namespace MindFlavor.SQLServerExporter
{
    public class FileHandler
    {
        public static void Handler(IApplicationBuilder app)
        {
            app.Run(async context =>
            {
                if (context.Request.Method != "GET")
                {
                    context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
                    return;
                }

                string s = null;
                using (FileStream fs = new FileInfo("C:\\tmp\\server-metrics-collectd_rev1.json").Open(FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    using (StreamReader sr = new StreamReader(fs))
                    {
                        s = await sr.ReadToEndAsync();
                    }
                }

                context.Response.StatusCode = StatusCodes.Status200OK;
                await context.Response.WriteAsync(s);
            });
        }
    }
}