using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GradingCommentary.Code
{
    public abstract class DisplayAdapter : IDisplayAdapter
    {
        private const int PointLength = 800;
        private static readonly string[] PointSize = { "x-small", "x-small", "x-small", "x-small"};

        public string ToString(IDictionary<Problem, Grade.Comment> problems, bool canDisplayAllGrades)
        {
            var sb = new StringBuilder();
            var total = Grade.GetPointsString(GetTotal(problems), GetTotalPoints(problems));

            foreach (var problem in Grade.GetDisplayedProblems(problems, canDisplayAllGrades))
            {
                sb.AppendLine();
                var comments = string.IsNullOrWhiteSpace(problem.Value.Comments)
                    ? ""
                    : WebUtility.HtmlEncode(problem.Value.Comments.Trim());

                if (problem.Value.Problem.IsNote())
                {
                    sb.AppendFormat(NoteFormat,
                        WebUtility.HtmlEncode(
                            string.IsNullOrEmpty(problem.Key.Description) ?
                            problem.Key.DisplayType :
                            problem.Key.Description
                            ),
                        string.IsNullOrWhiteSpace(problem.Value.Comments)
                    ? "" : problem.Value.Comments.Trim(),
                        comments);
                }
                else
                {
                    sb.AppendFormat(problem.Key.IsMultiplier() ? RatioBodyFormat : BodyFormat,
                        WebUtility.HtmlEncode(problem.Value.Problem.ToString()),
                        WebUtility.HtmlEncode(problem.Value.GetPointsString()),
                        problem.Value.Value,
                        WebUtility.HtmlEncode(String.Format(problem.Key.Description.Trim(), problem.Value.Points,
                        problem.Key.TotalMultiplier)),
                        comments);
                }
            }
            sb.AppendFormat(FooterFormat, total);
            var text = sb.ToString();
            return string.Format("{0}{1}",
                String.Format(HeaderFormat, total, canDisplayAllGrades ? "Rubrics" : "Mistakes", FitText(text)), text);
        }

        protected abstract string HeaderFormat { get; }
        protected abstract string FooterFormat { get; }
        protected abstract string RatioBodyFormat { get; }
        protected abstract string BodyFormat { get; }

        protected abstract string NoteFormat { get; }

        public decimal GetTotal(IDictionary<Problem, Grade.Comment> problems)
        {
            return Grade.CalculatePoints(problems);
        }

        public decimal GetTotalPoints(IDictionary<Problem, Grade.Comment> problems)
        {
            return Problem.CalculateTotalPoints(problems.Keys);
        }

        protected string FitText(string text)
        {
            var strippedHtml = Regex.Replace(text, "<[^>]+>", "");
            var size = strippedHtml.Length / PointLength;
            var point = size;
            return String.Format("font-size:{0}", point < PointSize.Length ? PointSize[point] : PointSize.Last());
        }
    }
}
