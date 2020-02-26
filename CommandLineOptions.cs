using System.Collections.Generic;
using Newtonsoft.Json;

public class CommandLineOptions
{
    [JsonProperty(PropertyName = "instances")]
    public List<SQLServerInstance> Instances { get; set; } = new List<SQLServerInstance>();

    [JsonProperty(PropertyName = "port")]
    public int Port { get; set; }

    [JsonProperty(PropertyName = "waitStats")]
    public WaitStats WaitStats { get; set; } = new WaitStats();

    [JsonProperty(PropertyName = "performanceCounters")]
    public PerformanceCounters PerformanceCounters { get; set; } = new PerformanceCounters();
}

public class PerformanceCounters
{
    [JsonProperty(PropertyName = "templateFiles")]
    public string[] TemplateFiles { get; set; } = new string[0];


}

public class SQLServerInstance
{
    [JsonProperty(PropertyName = "connectionString")]
    public string ConnectionString { get; set; } = string.Empty;
}

public class WaitStats
{
    [JsonProperty(PropertyName = "templateFiles")]
    public string[] TemplateFiles { get; set; } = new string[0];

}