using System.Collections.Generic;
using System.Threading.Tasks;
using System.Data.SqlClient;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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

        public string QueryAndSerializeData()
        {
            System.Text.StringBuilder sbCustomCounter = new System.Text.StringBuilder();
            using (SqlConnection conn = new SqlConnection(this.SQLServerInfo.ConnectionString))
            {
                logger.LogDebug($"About to open connection to {this.SQLServerInfo.Name}");
                conn.Open();


                using (SqlCommand cmd = new SqlCommand(this.Configuration.TSQL, conn))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var attributes = new List<string>();
                            foreach (var header in Configuration.Attributes)
                            {
                                attributes.Add((string)reader[header]);
                            }

                            foreach (var value in Configuration.Values)
                            {
                                var valueFromReader = reader[value.Value];

                                sbCustomCounter.Append($"{value.Name}{{instance=\"{this.SQLServerInfo.Name}\"");
                                for (int i = 0; i < attributes.Count; i++)
                                {
                                    sbCustomCounter.Append($", {Configuration.Attributes[i]}=\"{attributes[i]}\"");
                                }

                                sbCustomCounter.Append($"}} {valueFromReader}\n");

                            }
                        }
                    }
                }
            }

            // TODO: Add HELP section
            var s = sbCustomCounter.ToString();
            return s;
        }
    }
}
