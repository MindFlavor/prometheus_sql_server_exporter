using System.Collections.Concurrent;

namespace MindFlavor.Prometheus;

public class Metric
{
    public string Name { get; set; }
    public string? Help { get; set; }
    public string CounterType { get; set; }

    public ConcurrentBag<Instance> Instances { get; set; } = new ConcurrentBag<Instance>();

    public Metric(string Name, string? Help, string CounterType)
    {
        this.Name = Name;
        this.Help = Help;
        this.CounterType = CounterType;
    }

    public string Render()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        sb.Append($"# TYPE {Name} {CounterType}\n");
        if (Help != null)
            sb.Append($"# HELP {Help}\n");

        foreach (var instance in Instances)
        {
            sb.Append($"{Name} {instance.Render()}\n");
        }

        return sb.ToString();
    }
}