using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace MindFlavor.Prometheus;

public class ConcurrentMetricDictionary
{
    Mutex mutex = new Mutex();
    Dictionary<string, Metric> dict = new Dictionary<string, Metric>();

    private void MergeNonThreadSafe(Metric metricToMerge)
    {
        if (dict.TryGetValue(metricToMerge.Name, out Metric? metric))
        {
            metricToMerge.Instances.ForEach(instance => metric.Instances.Add(instance));
        }
        else
        {
            dict[metricToMerge.Name] = metricToMerge;
        }
    }

    public void Merge(Metric metricToMerge)
    {
        try
        {
            mutex.WaitOne();
            MergeNonThreadSafe(metricToMerge);
        }
        finally { mutex.ReleaseMutex(); }
    }
    public void Merge(IEnumerable<Metric> metricsToMerge)
    {
        try
        {
            mutex.WaitOne();
            foreach (var metricToMerge in metricsToMerge)
            {
                MergeNonThreadSafe(metricToMerge);
            }
        }
        finally { mutex.ReleaseMutex(); }
    }

    protected List<Metric> Materialize()
    {
        try
        {
            mutex.WaitOne();
            return dict.Select(i => i.Value).ToList();
        }
        finally { mutex.ReleaseMutex(); }
    }

    public string Render(bool fSorted = false)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        var list = Materialize();

        if (fSorted)
        {
            list.Sort((a, b) => a.Name.CompareTo(b.Name));
        }

        foreach (var metric in Materialize())
        {
            sb.Append(metric.Render());
        }
        return sb.ToString();
    }
}