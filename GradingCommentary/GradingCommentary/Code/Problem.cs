using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace GradingCommentary.Code
{
    public class Problem
    {
        private static int Ordering = 0;
        public readonly string DisplayType;
        public readonly string Type;
        public readonly int Number;
        public readonly decimal TotalMultiplier;
        public readonly decimal TotalPoints;
        public readonly string Description;
        public readonly bool IsMixed;
        public readonly int Order;

        public Problem(string type, int number, decimal totalPoints, string description, bool isMixed)
        {
            Order = Ordering++;
            IsMixed = isMixed;
            Type = IsMixed ? String.Empty : type;
            DisplayType = type;
            Number = number;
            Description = description;
            if (IsMultiplier(totalPoints))
            {
                TotalPoints = 0;
                TotalMultiplier = totalPoints;
            }
            else
            {
                TotalPoints = totalPoints;
                TotalMultiplier = 1;
            }
        }

        public Problem(Problem problem, int number)
        {
            Order = Ordering++;
            Description = problem.Description;
            DisplayType = problem.DisplayType;
            IsMixed = problem.IsMixed;
            Number = number;
            Type = problem.Type;
            TotalMultiplier = problem.TotalMultiplier;
            TotalPoints = problem.TotalPoints;
        }

        public bool IsTotal()
        {
            return DisplayType == Settings.Default.Total;
        }

        public bool IsNote()
        {
            return DisplayType == Settings.Default.Note;
        }

        public bool Visible
        {
            get
            {
                return !IsTotal();
            }
        }

        public bool IsMultiplier()
        {
            return IsMultiplier(TotalMultiplier);
        }
        protected bool Equals(Problem other)
        {
            return string.Equals(Type, other.Type) && Number == other.Number;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            var other = obj as Problem;
            return other != null && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Type != null ? Type.GetHashCode() : 0)*397) ^ Number;
            }
        }

        public static Problem GetProblem(string problemNumber, bool isMixed, decimal totalPoints, string description = "")
        {
            var str = problemNumber.Split(Settings.Default.ProblemSeparator);
            return new Problem(str[0], int.Parse(str[1]), totalPoints, description, isMixed);
        }

        private string ToString(string type)
        {
            return String.Format("{0}{2}{1}", type, Number, Settings.Default.ProblemSeparator);
        }

        public override string ToString()
        {
            return ToString(DisplayType);
        }

        public string ToNumberString()
        {
            return ToString(Type);
        }

        public bool Equals(int number, string type)
        {
            return Number == number && Type == type;
        }

        public Problem GetProblem(string problemNumber, decimal totalPoints)
        {
            return GetProblem(problemNumber, IsMixed, totalPoints);
        }

        public string GetNumber(string problemNumber)
        {
            return GetProblem(problemNumber, 0m).ToNumberString();
        }

        public static bool IsDisplayType(string typeField)
        {
            var display = Settings.Default.DisplayTypeField;
            return
                Enumerable.Range(0, display.Count)
                    .Any(i => display[i].Equals(typeField, StringComparison.InvariantCultureIgnoreCase));
        }

        public static decimal CalculateTotalPoints(IEnumerable<Problem> problems)
        {
            if (problems == null) throw new ArgumentNullException("problems");
            var totalProblem = problems.FirstOrDefault(p => p.IsTotal());
            if (totalProblem != null)
            {
                return totalProblem.TotalPoints;
            }
            var sum = problems.Sum(x => x.TotalPoints);
            return sum > 0m ? sum : 0m;
        }

        public static bool IsMultiplier(decimal d)
        {
            return d > 0m && d < 0.5m;
        }

        private string[] _displayTypeField;
    }
}
