using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GradingCommentary.Code
{
    public interface IDisplayAdapter
    {
        string ToString(IDictionary<Problem, Grade.Comment> problems, bool canDisplayAllGrades);
    }
}
