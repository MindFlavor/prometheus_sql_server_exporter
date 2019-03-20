using System.Collections.Generic;
using Newtonsoft.Json;

public class CommandLineOptions
{
    [JsonProperty(PropertyName = "instances")]
    public List<SQLServerInstance> Instances { get; set; }

    [JsonProperty(PropertyName = "port")]
    public int Port { get; set; }

    [JsonProperty(PropertyName = "waitStats")]
    public WaitStats WaitStats { get; set; }

    [JsonProperty(PropertyName = "performanceCounters")]
    public PerformanceCounters PerformanceCounters { get; set; }
}

public class PerformanceCounters
{
    [JsonProperty(PropertyName = "templateFiles")]
    public string[] TemplateFiles { get; set; }


}

public class SQLServerInstance
{
    [JsonProperty(PropertyName = "connectionString")]
    public string ConnectionString { get; set; }
}

public class WaitStats
{
    [JsonProperty(PropertyName = "templateFiles")]
    public string[] TemplateFiles { get; set; }

}