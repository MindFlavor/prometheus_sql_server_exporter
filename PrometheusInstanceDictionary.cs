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
                instance = new PrometheusInstance(type, value);
                instances.Add(type, instance);
            }
            instance.Add(value);
        }
    }
}
