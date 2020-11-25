using System.Data.SqlClient;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MindFlavor.SQLServerExporter
{
    public struct SQLServerInfo
    {
        public string ConnectionString;
        public string Name;
        public string Version;

    }

    public class SQLServerUtils
    {
        public static SQLServerInfo GetSQLServerInfo(HttpContext context, string connectionString)
        {
            var logger = context.RequestServices.GetRequiredService<ILogger<SQLServerUtils>>();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                logger.LogTrace($"Opening connection to {conn.ConnectionString}");
                conn.Open();

                using (SqlCommand cmd = new SqlCommand(TSQLStore.Entries["name_and_version"]["generic"], conn))
                {
                    logger.LogDebug($"Performing {cmd.CommandText}");
                    using (var reader = cmd.ExecuteReader())
                    {
                        reader.Read();
                        return new SQLServerInfo { ConnectionString = connectionString, Name = reader.GetString(0), Version = reader.GetString(1) };
                    }
                }
            }
        }
    }

}