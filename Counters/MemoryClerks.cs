using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Data.SqlClient;
using Microsoft.AspNetCore.Http;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MindFlavor.SQLServerExporter.Counters
{
    public class MemoryClerks
    {
        public SQLServerInfo SQLServerInfo;
        private ILogger<MemoryClerks> logger;

        private static HashSet<string>? Waits { get; set; }
        private static string TSQLQuery { get; set; } = string.Empty;

        public MemoryClerks(HttpContext context, SQLServerInfo sqlServerInfo)
        {
            this.SQLServerInfo = sqlServerInfo;
            logger = context.RequestServices.GetRequiredService<ILogger<MemoryClerks>>();

            TSQLQuery = TSQLStore.ProbeTSQL("memory_clerks", this.SQLServerInfo);
        }

        public async Task<string> QueryAndSerializeData()
        {
            using (SqlConnection conn = new SqlConnection(this.SQLServerInfo.ConnectionString))
            {
                logger.LogDebug($"About to open connection to {this.SQLServerInfo.Name}");
                await conn.OpenAsync();

                System.Text.StringBuilder sbSumPagesKB = new System.Text.StringBuilder();
                System.Text.StringBuilder sbSumVirtualMemoryReservedKB = new System.Text.StringBuilder();
                System.Text.StringBuilder sbSumVirtualMemoryCommittedKB = new System.Text.StringBuilder();
                System.Text.StringBuilder sbSumSharedMemoryReservedKB = new System.Text.StringBuilder();
                System.Text.StringBuilder sbSumSharedMemoryCommittedKB = new System.Text.StringBuilder();

                using (SqlCommand cmd = new SqlCommand(TSQLQuery, conn))
                {
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            string clerk_name = reader.GetString(0);
                            long sum_pages_kb = reader.GetInt64(1);
                            long sum_virtual_memory_reserved_kb = reader.GetInt64(2);
                            long sum_virtual_memory_committed_kb = reader.GetInt64(3);
                            long sum_shared_memory_reserved_kb = reader.GetInt64(4);
                            long sum_shared_memory_committed_kb = reader.GetInt64(5);


                            sbSumPagesKB.Append($"sql_memory_clerks_sum_pages_kb{{instance=\"{this.SQLServerInfo.Name}\", clerk=\"{clerk_name}\"}} {sum_pages_kb}\n");
                            sbSumVirtualMemoryReservedKB.Append($"sql_memory_clerks_sum_virtual_memory_reserved_kb{{instance=\"{this.SQLServerInfo.Name}\", clerk=\"{clerk_name}\"}} {sum_virtual_memory_reserved_kb}\n");
                            sbSumVirtualMemoryCommittedKB.Append($"sql_memory_clerks_sum_virtual_memory_committed_kb{{instance=\"{this.SQLServerInfo.Name}\", clerk=\"{clerk_name}\"}} {sum_virtual_memory_committed_kb}\n");
                            sbSumSharedMemoryReservedKB.Append($"sql_memory_clerks_sum_shared_memory_reserved_kb{{instance=\"{this.SQLServerInfo.Name}\", clerk=\"{clerk_name}\"}} {sum_shared_memory_reserved_kb}\n");
                            sbSumSharedMemoryCommittedKB.Append($"sql_memory_clerks_sum_shared_memory_committed_kb{{instance=\"{this.SQLServerInfo.Name}\", clerk=\"{clerk_name}\"}} {sum_shared_memory_committed_kb}\n");
                        }
                    }
                }

                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                sb.Append("# TYPE sql_memory_clerks_sum_pages_kb gauge\n");
                sb.Append(sbSumPagesKB);

                sb.Append("# TYPE sql_memory_clerks_sum_virtual_memory_reserved_kb gauge\n");
                sb.Append(sbSumVirtualMemoryReservedKB);

                sb.Append("# TYPE sql_memory_clerks_sum_virtual_memory_committed_kb gauge\n");
                sb.Append(sbSumVirtualMemoryCommittedKB);

                sb.Append("# TYPE sql_memory_clerks_sum_shared_memory_reserved_kb gauge\n");
                sb.Append(sbSumSharedMemoryReservedKB);

                sb.Append("# TYPE sql_memory_clerks_sum_shared_memory_committed_kb gauge\n");
                sb.Append(sbSumSharedMemoryCommittedKB);

                return sb.ToString();
            }
        }
    }
}
