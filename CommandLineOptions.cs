using System.Collections.Generic;
using System.Text.Json.Serialization;

public class CommandLineOptions
{
    [JsonPropertyName("instances")]
    public List<SQLServerInstance> Instances { get; set; } = new List<SQLServerInstance>();

    [JsonPropertyName("port")]
    public int Port { get; set; }

    [JsonPropertyName("waitStats")]
    public WaitStats WaitStats { get; set; } = new WaitStats();

    [JsonPropertyName("performanceCounters")]
    public PerformanceCounters PerformanceCounters { get; set; } = new PerformanceCounters();
}

public class PerformanceCounters
{
    [JsonPropertyName("templateFiles")]
    public string[] TemplateFiles { get; set; } = new string[0];


}

public class SQLServerInstance
{
    [JsonPropertyName("connectionString")]
    public string ConnectionString { get; set; } = string.Empty;
}

public class WaitStats
{
    [JsonPropertyName("templateFiles")]
    public string[] TemplateFiles { get; set; } = new string[0];

}