using System.Collections.Generic;
using System.Text;

namespace MindFlavor.SQLServerExporter
{
    public class PrometheusInstance
    {
        public string Name { get; }
        public string Type { get; }
        public string Help { get; }

        private List<string> values;

        public PrometheusInstance(string name, string type, string help)
        {
            this.Name = name;
            this.Type = type;
            this.Help = help;
            this.values = new List<string>();
        }

        public void Add(string value) { this.values.Add(value); }

        public void Merge(PrometheusInstance pi)
        {
            if (this.Name != pi.Name)
                throw new System.ArgumentException("incompatible PrometheusInstances");

            this.values.AddRange(pi.values);
        }

        public string SerializeHeader()
        {
            return $"HELP {Help}\nTYPE {Name} {Type}\n";
        }

        public string SerializeValues()
        {
            StringBuilder sb = new StringBuilder();
            values.ForEach(item => sb.Append($"{item}\n"));
            return sb.ToString();
        }
    }
}
