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

        public async Task<string> QueryAndSerializeData()
        {
            using (SqlConnection conn = new SqlConnection(this.SQLServerInfo.ConnectionString))
            {
                logger.LogDebug($"About to open connection to {this.SQLServerInfo.Name}");
                await conn.OpenAsync();

                System.Text.StringBuilder sbCustomCounter = new System.Text.StringBuilder();

                using (SqlCommand cmd = new SqlCommand(this.Configuration.TSQL, conn))
                {
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var attributes = new List<string>();
                            foreach (var header in Configuration.Attributes)
                            {
                                attributes.Add((string)reader[header]);
                            }
                            var value = reader[Configuration.Value];

                            sbCustomCounter.Append($"{Configuration.Name}{{instance=\"{this.SQLServerInfo.Name}\"");
                            for (int i = 0; i < attributes.Count; i++)
                            {
                                sbCustomCounter.Append($", {Configuration.Attributes[i]}=\"{attributes[i]}\"");
                            }

                            sbCustomCounter.Append($"}} {value}\n");
                        }
                    }
                }

                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                sb.Append($"# TYPE {Configuration.Name} {Configuration.CounterType}\n");
                sb.Append($"# HELP {Configuration.HelpText}\n");
                sb.Append(sbCustomCounter);

                return sb.ToString();
            }
        }
    }
}
