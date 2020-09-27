using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace GradingCommentary.Code
{
    class Problem
    {
        public readonly string Type;
        public readonly int Number;
        public readonly string Description;
        public readonly int TotalPoints;

        public Problem(string type, int number, string description, int total)
        {
            Type = type;
            Number = number;
            Description = description;
            TotalPoints = total;
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

        public override string ToString()
        {
            return String.Format("{0}#{1}", Type, Number);
        }

        public static int GetProblemNumber(string problemId)
        {
            var problem = problemId.Substring(problemId.IndexOf('#') + 1);
            int problemNumber;
            return int.TryParse(problem, out problemNumber) ? problemNumber : 0;
        }
    }
}
