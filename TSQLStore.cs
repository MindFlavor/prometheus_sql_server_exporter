using System;
using System.Linq;
using System.Collections.Generic;

namespace MindFlavor.SQLServerExporter
{
    public class TSQLStore
    {
        public static string PREFIX = "MindFlavor.SQLServerExporter.embed.sql";
        private static Dictionary<string, Dictionary<string, string>> _dic = new Dictionary<string, Dictionary<string, string>>();

        public static Dictionary<string, Dictionary<string, string>> Entries { get { return _dic; } }

        static TSQLStore()
        {
            var r = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceNames()
                .Where(i => i.StartsWith(PREFIX))
                .Select(i =>
                {
                    // extract stream
                    using (System.IO.StreamReader sr = new System.IO.StreamReader(System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(i)))
                    {
                        return new Tuple<string, string>(i, sr.ReadToEnd());
                    }
                })
                .Select(tuple => new Tuple<string, string>(tuple.Item1.Substring(PREFIX.Length + 1), tuple.Item2));

            var a = r.Select(tuple =>
                {
                    var tokens = tuple.Item1.Split('.').Select(i => { if (i.StartsWith("_")) return i.Substring(1); else return i; }).ToArray();
                    return new Tuple<string, string, string>(
                        String.Join('.', tokens, 0, tokens.Length - 2)
                        , tokens[tokens.Length - 2], tuple.Item2);
                });

            foreach (var t in a)
            {
                if (!_dic.ContainsKey(t.Item2))
                    _dic[t.Item2] = new Dictionary<string, string>();
                _dic[t.Item2][t.Item1] = t.Item3;
            }
        }

        public static string ProbeTSQL(string name, SQLServerInfo info)
        {
            return ProbeTSQL(name, info.Version);
        }

        public static string ProbeTSQL(string name, string version)
        {
            var entries = Entries[name];

            string[] tokens = version.Split('.');
            for (int i = tokens.Length; i >= 0; i--)
            {
                string versionMangled = string.Join(".", tokens, 0, i);
                if (entries.ContainsKey(versionMangled))
                    return entries[versionMangled];
            }

            if (entries.ContainsKey("generic"))
                return entries["generic"];

            throw new ArgumentOutOfRangeException("Cannot find script " + name);
        }
    }
}