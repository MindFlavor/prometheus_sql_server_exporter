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

        public void Add(string name, string type, string help, string value)
        {
            PrometheusInstance? instance;
            if (!instances.TryGetValue(name, out instance))
            {
                instance = new PrometheusInstance(name, type, help);
                instances.Add(name, instance);
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
                    instance = new PrometheusInstance(kvp.Value.Name, kvp.Value.Type, kvp.Value.Help);
                    instances.Add(kvp.Value.Name, instance);
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
