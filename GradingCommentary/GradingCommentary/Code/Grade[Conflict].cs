using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection.Emit;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GradingCommentary.Code
{
    class Grade 
    {
        private const string Header =
            "<table><tr><th>No.</th><th>Reference</th><th>Points</th><th>Max</th><th>Comments</th></tr>";

        private const string Footer = "<tr><th colspan=\"2\">Total</th><th>{0}</th><th>{1}</th><td></td></tr></table>";
        private const string Body = "<tr><th>{0}</th><td>{1}</td><td>{2}</td><td>{3}</td><td>{4}</td></tr>";

        private const string CommentHeader = "Feedback to Learner";
        private const string CommentFormatHeader = "Feedback Format";
        private const string FormatText = "HTML";
        public class Comment
        {
            public readonly int Points;
            public readonly string Comments;
            public readonly string ProblemId;

            public Comment(string problemId, int points, string comments)
            {
                ProblemId = problemId;
                Points = points;
                Comments = comments;
            }

            public Comment(string problemId, string combined)
            {
                ProblemId = problemId;
                var split = combined.Split(new[] {'\t', '\r', '\n', ' '}, 2, StringSplitOptions.RemoveEmptyEntries);
                int points;
                if (!split.Any() || !int.TryParse(split[0].Trim(), out points))
                {
                    Points = 0;
                    Comments = "";
                }
                else
                {
                    Points = points;
                    Comments = split.Length > 1 ? split[1].Trim() : "";
                }
            }

            public override string ToString()
            {
                return String.IsNullOrWhiteSpace(Comments) ? Points.ToString() : String.Format("{0} {1}", Points, Comments);
            }
        }

        public readonly string Type;
        private readonly IDictionary<Problem, Comment> _problems = new Dictionary<Problem, Comment>();
        private readonly ProblemMapping _problemMapping;
        public readonly IDictionary<string, string> OriginalStudent;
        public readonly IDictionary<string, string> Student;
        public Grade(ProblemMapping problems, IDictionary<string, string> student)
        {
            Type = student[problems.TypeField];
            student.Remove(new KeyValuePair<string, string>(problems.TypeField, Type));
            if (string.IsNullOrWhiteSpace(Type) || !problems.ContainsKey(Type))
            {
                Type = null;
                return;
            }
            _problemMapping = problems;
            var studentProblems = problems[Type];
            OriginalStudent = new Dictionary<string, string>(student);
            Student = new Dictionary<string, string>(student);
            foreach (var problem in studentProblems)
            {
                string grade;
                if (!Student.TryGetValue(problem.Key, out grade))
                {
                    grade = "0";
                }
                _problems[problem.Value] = new Comment(problem.Key, grade);
                Student.Remove(new KeyValuePair<string, string>(problem.Key, grade));
                foreach (var kvp in Student)
                {
                    var split = kvp.Key.Split(new char[] {'|'}, StringSplitOptions.RemoveEmptyEntries);
                    if (split.Count() < 2) continue;
                    var id = split.LastOrDefault();
                    int identity;
                    if (string.IsNullOrWhiteSpace(id) || !int.TryParse(id, out identity)) continue;
                    Student[kvp.Key] = Total.ToString();
                    break;
                }
                var comments = ToString();
                if (!string.IsNullOrWhiteSpace(comments))
                {
                    Student[CommentHeader] = comments;
                    Student[CommentFormatHeader] = FormatText;
                }
            }
        }

        public IDictionary<string, string> PopulateDefaultGrades(IDictionary<string, string> student = null)
        {
            if (student == null) student = new Dictionary<string, string>(OriginalStudent);
            student[_problemMapping.TypeField] = _problemMapping.DefaultType;
            foreach (var problem in _problems)
            {
                var key = problem.Key.ToString();
                student[key] = problem.Value.Points.ToString();
            }
            return student;
        }

        public int Total
        {
            get { return _problems.Sum(x => x.Value.Points); }
        }

        public int MaxPoints
        {
            get { return _problems.Sum(x => x.Key.TotalPoints); }
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append(Header);
            foreach (var problem in _problems.OrderBy(x => x.Key.Type).ThenBy(x=>Problem.GetProblemNumber(x.Value.ProblemId)))
            {
                var comments = string.IsNullOrWhiteSpace(problem.Value.Comments)
                    ? ""
                    : WebUtility.HtmlEncode(problem.Value.Comments).Replace("\r\n", "<br />").Replace("\n", "<br />");
                sb.AppendFormat(Body, problem.Value.ProblemId, problem.Key.Description, problem.Value.Points,
                    problem.Key.TotalPoints > 0 ? problem.Key.TotalPoints.ToString() : "", comments);
            }
            sb.AppendFormat(Footer, Total, MaxPoints > 0 ? MaxPoints.ToString() : "");
            return sb.ToString();
        }


        public static IEnumerable<Grade> FetchAll(ProblemMapping problems, IEnumerable<IDictionary<string, string>> students)
        {
            return
                from student in students
                let grade = new Grade(problems, student)
                where grade.Type != null && problems.ContainsKey(grade.Type)
                select grade;
        }
    }
}
