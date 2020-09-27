using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Automation.Peers;
using System.Windows.Documents;

namespace GradingCommentary.Code
{
    class ProblemMapping : IReadOnlyDictionary<string, IReadOnlyDictionary<string, Problem>>
    {
        public IReadOnlyDictionary<string, Problem> ReferencedProblem { get; private set; }
        public const string TotalField = "Total";
        public string TypeField { get; private set; }
        public string DefaultType { get { return Keys.First(); } }
        public string DescriptionField { get; private set; }
        public ProblemMapping(string text)
        {
            var data = FetchDictionary(text);
            var problems = new HashSet<Problem>();
            foreach (var row in data)
            {
                string totalString;
                int total = row.TryGetValue(TotalField, out totalString) ? int.Parse(totalString) : 0;
                var problem = row.Take(2).ToArray();
                TypeField = problem[0].Key;
                var description = row.Last();
                DescriptionField = description.Key;
                IReadOnlyDictionary<string, Problem> mapping;
                var problemObject = new Problem(problem[0].Value, Int32.Parse(problem[1].Value), description.Value, total);
                if (!TryGetValue(problem[1].Key, out mapping))
                {
                    _dictionary[problem[1].Key] = mapping = new Dictionary<string, Problem>();
                }
                ((IDictionary<string, Problem>) mapping).Add(problemObject.ToString(), problemObject);
                ReferencedProblem = mapping;
                problems.Add(problemObject);
            }
            int i = 0;
            foreach(var problem in problems)
            {
                var row = data[i];
                var type = row.First().Value;
                var last = row.Last();
                var rowsModified = 
                    from r in row
                    where last.Key != r.Key && r.Key != TotalField
                    select r;
                foreach (var kvp in rowsModified.Skip(2))
                {
                    var mappedProblem = problems.First(p => p.Number == Int32.Parse(kvp.Value) && p.Type == type);
                    IReadOnlyDictionary<string, Problem> mapping;
                    if (!_dictionary.TryGetValue(kvp.Key, out mapping))
                    {
                        _dictionary[kvp.Key] = mapping = new Dictionary<string, Problem>();
                    }
                    ((IDictionary<string, Problem>)mapping).Add(mappedProblem.ToString(), problem);
                }
                i++;
            }
        }

        internal static IList<IDictionary<string, string>> FetchDictionary(string text)
        {
            using (var sr = new StringReader(text))
            using (var parser = new CsvHelper.CsvParser(sr))
            {
                parser.Configuration.Delimiter = "\t";
                var list = new List<IDictionary<string, string>>();
                string[] headers = parser.Read();
                for (;;)
                {
                    var dict = new Dictionary<string, string>();
                    var data = parser.Read();
                    if (data == null || data.Length < 1)
                    {
                        break;
                    }
                    for (int i = 0; i < headers.Length; i ++)
                    {
                        dict[headers[i]] = data[i];
                    }
                    list.Add(dict);
                }
                return list;
            }
        }

        internal static string ToCsv(ICollection<IDictionary<string, string>> data, string delimiter = ",")
        {
            var factory = new CsvHelper.CsvFactory();
            using (StringWriter sw = new StringWriter())
            using (var writer = factory.CreateWriter(sw))
            {
                writer.Configuration.Delimiter = delimiter;
                foreach (var kvp in data.First())
                {
                    writer.WriteField(typeof(string), kvp.Key);
                }
                foreach (var d in data)
                {
                    writer.NextRecord();
                    foreach (var kvp in d)
                    {
                        writer.WriteField(typeof (string), kvp.Value);
                    }
                }
                writer.NextRecord();

                return sw.ToString();
            }
/*            var sb = new StringBuilder();
            bool firstheader = true;
            foreach (var kvp in data.First())
            {
                sb.AppendFormat(firstheader ? @"""{0}""" : @",""{0}""", kvp.Key);
                firstheader = false;
            }
            foreach (var d in data)
            {
                sb.AppendLine();
                bool first = true;
                foreach (var kvp in d)
                {
                    sb.AppendFormat(first ? @"""{0}""" : @",""{0}""", kvp.Value);
                    first = false;
                }
            }
            return sb.ToString();*/
        }

        internal ICollection<IDictionary<string, string>> GetFillers(string studentsText)
        {
            var students = FetchDictionary(studentsText);
            foreach (var student in students)
            {
                if (!student.ContainsKey(TypeField))
                    student[TypeField] = "";
                foreach (var problem in ReferencedProblem)
                {
                    var key = problem.Value.ToString();
                    if (!student.ContainsKey(key))  
                        student[key] = "";
                }
            }
            return students;
        }

        private readonly Dictionary<string, IReadOnlyDictionary<string, Problem>> _dictionary =
            new Dictionary<string, IReadOnlyDictionary<string, Problem>>();
        public IEnumerator<KeyValuePair<string, IReadOnlyDictionary<string, Problem>>> GetEnumerator()
        {
            return _dictionary.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int Count { get { return _dictionary.Count; } }
        public bool ContainsKey(string key)
        {
            return _dictionary.ContainsKey(key);
        }

        public bool TryGetValue(string key, out IReadOnlyDictionary<string, Problem> value)
        {
            return _dictionary.TryGetValue(key, out value);
        }

        public IReadOnlyDictionary<string, Problem> this[string key]
        {
            get { return _dictionary[key]; }
        }

        public IEnumerable<string> Keys { get { return _dictionary.Keys; } }
        public IEnumerable<IReadOnlyDictionary<string, Problem>> Values { get { return _dictionary.Values; } }
    }
}
