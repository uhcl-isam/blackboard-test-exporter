using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GradingCommentary.Code
{
    public interface IProcessor
    {
        IList<IDictionary<string, string>> FetchProblems();
        IList<IDictionary<string, string>> FetchStudents();
    }
}
