using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Schema;
using Microsoft.Win32;

namespace GradingCommentary.Code
{
    class ExcelProcessor : IProcessor, IDisposable
    {
        private readonly OleDbConnection _connection;
        private IDictionary<string, string>[] _gradedStudents;
        public ExcelProcessor(string excelFilePath)
        {
            var connectionString = String.Format(Settings.Default.ExcelConnectionString, excelFilePath);
            _connection = new OleDbConnection(connectionString);
        }

        private void Open()
        {
            if (_connection.State == ConnectionState.Closed)
                _connection.Open();
        }

        private IList<IDictionary<string, string>> FetchDictionary(string worksheetName)
        {
            worksheetName += "$";
            var result = new List<IDictionary<string, string>>();
            Open(); /*
            DataTable columns = _connection.GetOleDbSchemaTable(OleDbSchemaGuid.Columns,
                new object[] {null, null, worksheetName, null});
            if (columns == null) throw new Exception(String.Format("{0} Worksheet does not exist!", worksheetName));
            var columnNames =
                from DataRow row in columns.Rows
                select string.Format("[{0}]", row["Column_Name"]); */
            string sql = String.Format("select * from [{0}]", worksheetName);//, string.Join(", ",columnNames));
            using (var command = new OleDbCommand(sql, _connection))
            using (var reader = command.ExecuteReader())
            {
                if (reader != null)
                {
                    while (reader.Read())
                    {
                        var dict = new Dictionary<string, string>();
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            var name = reader.GetName(i).Replace('(', '[').Replace(')', ']');
                            var value = reader.GetValue(i);
                            dict[name] = value == null ? "" : value.ToString();
                        }
                        result.Add(dict);
                    }
                }
            }
            return result;
        }

        public IList<IDictionary<string, string>> FetchProblems()
        {
            var d = FetchDictionary(Settings.Default.ProblemsSheetName);
            return d;
        }

        public IList<IDictionary<string, string>> FetchStudents()
        {
            var d = FetchDictionary(Settings.Default.StudentsSheetName);
            return d;
        }

        public void Dispose()
        {
            _connection.Dispose();
        }
    }
}
