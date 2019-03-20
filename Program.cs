using System;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace MindFlavor.SQLServerExporter
{
    public class Program
    {
        public static CommandLineOptions CommandLineOptions { get; private set; }
        public static void Main(string[] args)
        {
            var assemblyVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            System.Console.WriteLine($"Prometheus SQL Server Exporter v{assemblyVersion.ToString()}");
            System.Console.WriteLine($"Licensed under Apache License 2.0\n");

            string configFile = null;
            for (int i = 0; (i < args.Length - 1 && configFile == null); i++)
                if (args[i].ToLower() == "-c")
                    configFile = args[i + 1];

            if (configFile == null)
            {
                Console.WriteLine("Syntax error:\nMissing -c <config file> parameter");
                return;
            }

            string jsonContents = null;
            using (System.IO.StreamReader sr = new StreamReader(new System.IO.FileStream(configFile, FileMode.Open, FileAccess.Read, FileShare.Read)))
            {
                jsonContents = sr.ReadToEnd();
            }

            CommandLineOptions = JsonConvert.DeserializeObject<CommandLineOptions>(jsonContents);

            var host = CreateWebHostBuilder(args).Build();
            var logger = host.Services.GetRequiredService<ILogger<Program>>();
            host.Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseUrls($"http://*:{CommandLineOptions.Port}")
                .UseStartup<Startup>();
    }
}
