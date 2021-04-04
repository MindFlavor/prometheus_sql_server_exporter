using System.Collections.Generic;
using System.Text;

namespace MindFlavor.SQLServerExporter
{
    public class PrometheusInstance
    {
        public string Type { get; }
        public string Help { get; }

        private List<string> values;

        public PrometheusInstance(string type, string help)
        {
            this.Type = type;
            this.Help = help;
            this.values = new List<string>();
        }

        public void Add(string value) { this.values.Add(value); }

        public string SerializeValues()
        {
            StringBuilder sb = new StringBuilder();
            values.ForEach(item => sb.Append($"{item}\n"));
            return sb.ToString();
        }
    }
}
