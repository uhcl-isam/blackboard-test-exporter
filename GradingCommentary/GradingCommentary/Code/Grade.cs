using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Reflection.Emit;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Office.Interop.Excel;

namespace GradingCommentary.Code
{
    public class Grade 
    {
        private static IDisplayAdapter _displayAdapter ;
        public readonly string Id;
        public readonly bool CanDisplayAllGrades;
        public readonly IDictionary<string, string> Student;
        private readonly IDictionary<Problem, Comment> _problems = new Dictionary<Problem, Comment>();
        private readonly ProblemMapping _problemMapping;

        public Grade(ProblemMapping problems, IDictionary<string, string> studentGrade, bool canDisplayAllGrades, bool relativeGrades)
        {
            _commentHeader = Settings.Default.CommentHeader;
            _commentFormatHeader = Settings.Default.CommentFormatHeader;
            var formatText = Settings.Default.CommentFormat;

            CanDisplayAllGrades = canDisplayAllGrades;
            _problemMapping = problems;
            var student = new Dictionary<string, string>(studentGrade);
            Student = student;
            Id = student[Settings.Default.IdField];
            student.Remove(Settings.Default.IdField);
            if (String.IsNullOrWhiteSpace(Id) || !problems.ContainsKey(Id))
            {
                Id = null;
                return;
            }
            var studentProblems = problems[Id];
            foreach (var problem in studentProblems)
            {
                string grade;
                if (!student.TryGetValue(problem.Value.GetNumber(problem.Key), out grade))
                {
                    grade = "0";
                }
                var comment = new Comment(problem.Key, problem.Value.IsMixed, canDisplayAllGrades, relativeGrades, grade,
                    problem.Value.IsMultiplier() ? problem.Value.TotalMultiplier : problem.Value.TotalPoints);
                _problems[problem.Value] = comment;
                var numberString = comment.Problem.ToNumberString();
                student.Remove(numberString);
                foreach (var kvp in student)
                {
                    var split = kvp.Key.Split(new char[] {'|'}, StringSplitOptions.RemoveEmptyEntries);
                    if (split.Count() < 2) continue;
                    var id = split.LastOrDefault();
                    int identity;
                    if (String.IsNullOrWhiteSpace(id) || !Int32.TryParse(id, out identity)) continue;
                    student[kvp.Key] = Total.ToString("N2");
                    break;
                }
            }
            var comments = ToString();
            if (!String.IsNullOrWhiteSpace(comments))
            {
                student[_commentHeader] = comments;
                student[_commentFormatHeader] = formatText;
            }

            foreach(var key in student.Keys.ToArray())
            {
                var split = key.Split(new[] {Settings.Default.ProblemSeparator}, 2);
                int dummy;
                if (split.Length > 1 && int.TryParse(split[1], out dummy))
                {
                    student.Remove(key);
                }
            }
        }

        public static void SetDisplayAdapter(IDisplayAdapter adapter)
        {
            if (adapter == null) throw new ArgumentNullException("adapter");
            _displayAdapter = adapter;
        }

        public void Repopuplate(IDictionary<string, string> student)
        {
            foreach (var problem in _problems)
            {
                var key = problem.Key.ToString();
                student[key] = problem.Value.ToString();
            }
        }

        public decimal Total
        {
            get { return CalculatePoints(_problems); }
        }

        public sealed override string ToString()
        {
            return (_displayAdapter ?? DisplaySetting.DefaultDisplayAdapter).ToString(_problems, CanDisplayAllGrades);
        }

        public IDictionary<string, string> PopulateDefaultGrades()
        {
            var output = new Dictionary<string, string>(Student);
            output[Settings.Default.IdField] = _problemMapping.DefaultType;
            foreach (var problem in _problems)
            {
                output[problem.Key.ToString()] = problem.Value.Points.ToString(CultureInfo.InvariantCulture);
            }
            output.Remove(_commentHeader);
            output.Remove(_commentFormatHeader);
            return output;
        }

        public static IEnumerable<Grade> FetchAll(ProblemMapping problems, ICollection<IDictionary<string, string>> studentsGrade, bool canDisplayAllGrades, bool relativeGrades)
        {
            return
                from student in studentsGrade
                let grade = new Grade(problems, student, canDisplayAllGrades, relativeGrades)
                where grade.Id != null
                select grade;
        }

        public static IEnumerable<KeyValuePair<Problem, Comment>> GetDisplayedProblems(IDictionary<Problem, Comment> problems, bool showAllGrades)
        {
            var displayedProblems = problems
                .OrderBy(x => x.Value.Problem.IsNote())
                .ThenBy(x => x.Value.Problem.IsMixed ? "" : x.Value.Problem.Type)
                .ThenBy(x => x.Value.Problem.Number)
                .Where(
                    x =>
                    x.Value.Problem.Visible &&
                        ((!(x.Value.Problem.TotalPoints == 0m && x.Value.Points == 0m) &&
                         !(x.Value.Problem.IsMultiplier() && x.Value.Points == 0m) && (showAllGrades ||
                                                                                       x.Value.Points !=
                                                                                       x.Value.Problem.TotalPoints)) ||
                        !String.IsNullOrWhiteSpace(x.Value.Comments)));
            return displayedProblems;
        }

        public static decimal CalculatePoints(IDictionary<Problem, Comment> problems)
        {
            if (problems == null) throw new ArgumentNullException("problems");
            decimal ratio = 1m;
            decimal totalPoints = 0m;
            foreach (var problem in problems)
            {
                if (problem.Key.IsMultiplier())
                {
                    ratio *= problem.Value.GetMultiplier();
                }
                else
                {
                    totalPoints += problem.Value.Points;
                }
            }
            totalPoints *= ratio;
            return totalPoints > 0m ? totalPoints : 0m;
        }

        public static string GetPointsString(decimal points, decimal totalPoints)
        {
            return
                totalPoints <= 0
                    ? String.Format("{0:0.##}", points)
                    : String.Format("{0:0.##}/{1:0.##}, {2:P0}", points, totalPoints, points/totalPoints);
        }

        public class Comment
        {
            public readonly decimal Points;
            public readonly string Comments;
            public readonly Problem Problem;
            public readonly bool CanDisplayAllGrades;

            public Comment(string problem, bool onlyDisplayType, bool canDisplayAllGrades, bool relativeGrades, decimal points, decimal total, string comments)
                : this(Problem.GetProblem(problem, onlyDisplayType, total), canDisplayAllGrades, relativeGrades && total != 0m ? points * total : points, comments)
            {
            }

            private Comment(Problem problem, bool canDisplayAllGrades, decimal points, string comments)
            {
                CanDisplayAllGrades = canDisplayAllGrades;
                Problem = problem;
                Points = points;
                Comments = comments;
            }

            private Comment(Problem problem, bool canDisplayAllGrades, bool relativeGrades, params object[] p)
                :this(problem, canDisplayAllGrades, relativeGrades && problem.TotalPoints != 0m ? (decimal) p[0] * problem.TotalPoints : (decimal) p[0], (string) p[1])
            {
            }

            public Comment(string problem, bool onlyDisplayType, bool canDisplayAllGrades, bool relativeGrades, string combined, decimal total)
                : this(Problem.GetProblem(problem, onlyDisplayType, total), canDisplayAllGrades, relativeGrades, combined)
            {
            }

            public Comment(Problem problem, bool canDisplayAllGrades, bool relativeGrades, string combined)
                : this(problem, canDisplayAllGrades, relativeGrades, GetCombined(combined))
            {
            }

            private static object[] GetCombined(string combined)
            {
                decimal points;
                combined = combined.Trim();
                if (decimal.TryParse(combined, out points))
                    return new object[] {points, ""};
                var split = combined.Split(new[] {'\n', '\r', '\t', ' '}, 2, StringSplitOptions.RemoveEmptyEntries);
                if (split.Length < 2 || !decimal.TryParse(split[0], out points))
                    return new object[]{0m, combined};
                return new object[] {points, split[1]};
            }

            public override string ToString()
            {
                return String.IsNullOrWhiteSpace(Comments) ? GetPointsString() : String.Format("{0} {1}", GetPointsString(), Comments);
            }

            public string GetPointsString()
            {
                return Problem.IsMultiplier()
                    ? String.Format("{0:0.##} of {1:0.##%}", Points, Problem.TotalMultiplier)
                    : Grade.GetPointsString(Points, Problem.TotalPoints);
            }

            public decimal GetMultiplier()
            {
                return Problem.IsMultiplier() ? 1m - Problem.TotalMultiplier * Points : 1m;
            }

            public decimal Value
            {
                get { return Problem.IsMultiplier() ? GetMultiplier() : Points; }
            }
        }

        private readonly string _commentHeader;
        private readonly string _commentFormatHeader;
    }
}
