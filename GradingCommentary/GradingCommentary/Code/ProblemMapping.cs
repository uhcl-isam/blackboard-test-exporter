using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Automation.Peers;
using System.Windows.Documents;

namespace GradingCommentary.Code
{
    public class ProblemMapping : IReadOnlyDictionary<string, IReadOnlyDictionary<string, Problem>>
    {
        public IReadOnlyDictionary<string, Problem> ReferencedProblem { get; private set; }

        public string TypeField { get; private set; }
        public string DescriptionField { get; private set; }
        public string TotalField { get; private set; }

        private readonly Dictionary<string, IReadOnlyDictionary<string, Problem>> _dictionary =
            new Dictionary<string, IReadOnlyDictionary<string, Problem>>();

        public ProblemMapping(IProcessor processor)
            :this(processor.FetchProblems())
        {
        }

        public ProblemMapping(string text)
            : this(FetchDictionary(text))
        {
        }

        public ProblemMapping(IList<IDictionary<string, string>> data)
        {
            var problems = new HashSet<Problem>();
            foreach (var row in data)
            {
                var problem = row.Take(2).ToArray();
                TypeField = problem[0].Key;
                if (string.IsNullOrEmpty(DescriptionField))
                    DescriptionField = row.Keys.FirstOrDefault(IsInDescriptionFields);
                if (string.IsNullOrEmpty(TotalField))
                    TotalField = row.Keys.FirstOrDefault(IsInTotalFields);
                var description = DescriptionField == null ? "" : row[DescriptionField];
                decimal total = 0m;
                if (!string.IsNullOrEmpty(TotalField) && !decimal.TryParse(row[TotalField], out total))
                {
                    if (decimal.TryParse(row[TotalField].Trim('%'), out total))
                    {
                        total *= 0.01m;
                    }
                }
                IReadOnlyDictionary<string, Problem> mapping;
                int number = 0;
                Int32.TryParse(problem[1].Value, out number);
                var problemObject = new Problem(problem[0].Value, number, total,
                    description,
                    Problem.IsDisplayType(TypeField));
                if (!TryGetValue(problem[1].Key, out mapping))
                {
                    _dictionary[problem[1].Key] = mapping = new Dictionary<string, Problem>();
                }
                ((IDictionary<string, Problem>) mapping).Add(problemObject.ToString(), problemObject);
                ReferencedProblem = mapping;
                problems.Add(problemObject);
            }
            int i = 0;
            if (TotalField == null || DescriptionField == null)
                throw new InvalidOperationException("Total Field or Description Field cannot be undefined.");
            foreach(var problem in problems)
            {
                var row = data[i];
                foreach (var kvp in row.Skip(2))
                {
                    if (kvp.Key == DescriptionField || kvp.Key == TotalField)
                        break;
                    IReadOnlyDictionary<string, Problem> mapping;
                    if (!_dictionary.TryGetValue(kvp.Key, out mapping))
                    {
                        _dictionary[kvp.Key] = mapping = new Dictionary<string, Problem>();
                    }
                    ((IDictionary<string, Problem>)mapping).Add(new Problem(problem, int.Parse(kvp.Value)).ToString(), problem);
                }
                i++;
            }
        }

        private bool IsInTotalFields(string field)
        {
            return
                Settings.Default.TotalField.Cast<string>()
                    .Any(f => field.Trim().Trim('#').Equals(f.Trim(), StringComparison.InvariantCultureIgnoreCase));
        }

        private bool IsInDescriptionFields(string field)
        {
            return
                Settings.Default.DescriptionField.Cast<string>()
                    .Any(f => field.Trim().Trim('#').Equals(f.Trim(), StringComparison.InvariantCultureIgnoreCase));
        }


        internal static IList<IDictionary<string, string>> FetchDictionary(DataTable dataTable)
        {
            var list = new List<IDictionary<string, string>>();
            foreach (DataRow row in dataTable.Rows)
            {
                var r = new Dictionary<string, string>();
                list.Add(r);
                foreach (DataColumn column in dataTable.Columns)
                {
                    var value = row[column];
                    r[column.ColumnName] = value == null ? String.Empty : value.ToString();
                }
            }
            return list;
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

        internal ICollection<IDictionary<string, string>> GetFillers(IProcessor processor)
        {
            return GetFillers(processor.FetchStudents());
        }

        internal ICollection<IDictionary<string, string>> GetFillers(string studentsText)
        {
            return GetFillers(FetchDictionary(studentsText));
        }

        internal ICollection<IDictionary<string, string>> GetFillers(IList<IDictionary<string, string>> students)
        {
            foreach (var student in students)
            {
                if (!student.ContainsKey(Settings.Default.IdField))
                    student[Settings.Default.IdField] = "";
                foreach (var problem in ReferencedProblem)
                {
                    var key = problem.Value.ToNumberString();
                    if (!student.ContainsKey(key))  
                        student[key] = "";
                }
            }
            return students;
        }

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

        public string DefaultType
        {
            get
            {
                return this.FirstOrDefault().Key;
            }
        }

        public IEnumerable<string> Keys { get { return _dictionary.Keys; } }
        public IEnumerable<IReadOnlyDictionary<string, Problem>> Values { get { return _dictionary.Values; } }
    }
}
