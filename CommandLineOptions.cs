using System.Collections.Generic;
using System.Text.Json.Serialization;

public class CommandLineOptions
{


    [JsonPropertyName("instances")]
    public List<SQLServerInstance> Instances { get; set; } = new List<SQLServerInstance>();

    [JsonPropertyName("port")]
    public int Port { get; set; }

    [JsonPropertyName("instanceTotalTimeout")]
    public int InstanceTotalTimeout { get; set; }

    [JsonPropertyName("waitStats")]
    public WaitStats WaitStats { get; set; } = new WaitStats();

    [JsonPropertyName("performanceCounters")]
    public PerformanceCounters PerformanceCounters { get; set; } = new PerformanceCounters();

    [JsonPropertyName("customCounters")]
    public CustomCounters[] CustomCounters { get; set; } = new CustomCounters[0];
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

public class CustomCounters
{
    [JsonPropertyName("customCounter")]
    public CustomCounterConfiguration CustomCounter { get; set; } = new CustomCounterConfiguration();
}

public class CustomCounterConfiguration
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    [JsonPropertyName("tsql")]
    public string TSQL { get; set; } = string.Empty;
    [JsonPropertyName("attributes")]
    public string[] Attributes { get; set; } = new string[0];
    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;
    [JsonPropertyName("type")]
    public string CounterType { get; set; } = string.Empty;
    [JsonPropertyName("help_text")]
    public string HelpText { get; set; } = string.Empty;
}