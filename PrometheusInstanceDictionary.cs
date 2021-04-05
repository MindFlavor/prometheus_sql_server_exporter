using System.Collections.Generic;
using System.Text;

namespace MindFlavor.SQLServerExporter
{
    public class PrometheusInstanceDictionary
    {
        Dictionary<string, PrometheusInstance> instances;
        public PrometheusInstanceDictionary()
        {
            this.instances = new Dictionary<string, PrometheusInstance>();
        }

        public void Add(string type, string help, string value)
        {
            PrometheusInstance? instance;
            if (!instances.TryGetValue(type, out instance))
            {
                instance = new PrometheusInstance(type, help);
                instances.Add(type, instance);
            }
            instance.Add(value);
        }

        public void Merge(PrometheusInstanceDictionary dict)
        {
            foreach (var kvp in dict.instances)
            {
                PrometheusInstance? instance;
                if (!this.instances.TryGetValue(kvp.Key, out instance))
                {
                    instance = new PrometheusInstance(kvp.Value.Type, kvp.Value.Help);
                    instances.Add(kvp.Value.Type, instance);
                }

                instance.Merge(kvp.Value);
            }
        }

        public string SerializeAll()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var kvp in instances)
            {
                sb.Append(kvp.Value.SerializeHeader());
                sb.Append(kvp.Value.SerializeValues());
            }

            return sb.ToString();
        }
    }
}
