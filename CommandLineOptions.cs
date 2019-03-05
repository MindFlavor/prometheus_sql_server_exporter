using System.Collections.Generic;
using Newtonsoft.Json;

public class CommandLineOptions
{
    [JsonProperty(PropertyName = "instances")]
    public List<SQLServerInstanceOptions> Instances { get; set; }

    [JsonProperty(PropertyName = "port")]
    public int Port { get; set; }

    [JsonProperty(PropertyName = "waitStatsOptions")]
    public WaitStatsOptions WaitStatsOptions { get; set; }
}

public class SQLServerInstanceOptions
{
    [JsonProperty(PropertyName = "connectionString")]
    public string ConnectionString { get; set; }
}

public class WaitStatsOptions
{
    [JsonProperty(PropertyName = "waitStatsFiles")]
    public string[] WaitStatsFiles { get; set; }

}