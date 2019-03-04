using System.Collections.Generic;
using Newtonsoft.Json;

public class CommandLineOptions
{
    [JsonProperty(PropertyName = "instances")]
    public List<SQLServerInstanceOptions> Instances { get; set; }

    [JsonProperty(PropertyName = "port")]
    public int Port { get; set; }
}

public class SQLServerInstanceOptions
{
    [JsonProperty(PropertyName = "connectionString")]
    public string ConnectionString { get; set; }
}