using System.Collections.Generic;
using System.Text;

namespace MindFlavor.Prometheus;

public class Instance
{
    public List<KeyValuePair<string, string>> Attributes { get; } = new List<KeyValuePair<string, string>>();
    public string Value { get; set; } = string.Empty;

    public Instance(string sqlServerInstance)
    {
        Attributes.Add(new KeyValuePair<string, string>("instance", sqlServerInstance));
    }

    public string Render()
    {
        StringBuilder sb = new StringBuilder();
        Attributes.ForEach(kvp => sb.Append($"{kvp.Key}=\"{kvp.Value}\", "));
        sb.Length = sb.Length-2;

        return $"{{{sb.ToString()}}} {Value}";
    }
}