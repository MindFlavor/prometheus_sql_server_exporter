using System.Collections.Generic;
using System.Threading.Tasks;
using System.Data.SqlClient;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MindFlavor.Prometheus;

namespace MindFlavor.SQLServerExporter.Counters
{
    public class CustomCounter
    {
        public SQLServerInfo SQLServerInfo;
        private ILogger<CustomCounter> logger;

        private CustomCounterConfiguration Configuration { get; set; } = new CustomCounterConfiguration();

        public CustomCounter(HttpContext context, SQLServerInfo sqlServerInfo, CustomCounterConfiguration configuration)
        {
            this.SQLServerInfo = sqlServerInfo;
            logger = context.RequestServices.GetRequiredService<ILogger<CustomCounter>>();
            this.Configuration = configuration;
        }

        public void QueryAndAddToSharedMetricDictionary(ConcurrentMetricDictionary sharedMetricDictionary)
        {
            using SqlConnection conn = new SqlConnection(this.SQLServerInfo.ConnectionString);

            logger.LogDebug($"About to open connection to {this.SQLServerInfo.Name}");
            conn.Open();

            // build Metrics based on Configuration
            var metrics = new Prometheus.Metric[Configuration.Values.Length];
            for (int i = 0; i < metrics.Length; i++)
            {
                metrics[i] = new Prometheus.Metric(Configuration.Values[i].Name, Configuration.Values[i].HelpText, Configuration.Values[i].CounterType);
            }

            using SqlCommand cmd = new SqlCommand(this.Configuration.TSQL, conn);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {

                for (int i = 0; i < Configuration.Values.Length; i++)
                {
                    Prometheus.Instance instance = new Prometheus.Instance(this.SQLServerInfo.Name);
                    foreach (var header in Configuration.Attributes)
                    {
                        instance.Attributes.Add(new KeyValuePair<string, string>(header, (string)reader[header]));
                    }

                    instance.Value = reader[Configuration.Values[i].Value].ToString();

                    metrics[i].Instances.Add(instance);
                }
            }
            
            sharedMetricDictionary.Merge(metrics);
       }
    }
}
