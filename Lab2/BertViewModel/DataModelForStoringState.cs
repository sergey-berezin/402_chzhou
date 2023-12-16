using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BertViewModel
{
    public class AppState
    {
        public List<string> Texts { get; set; } = new List<string>();
        public Dictionary<string, string> QuestionAnswerPairs { get; set; } = new Dictionary<string, string>();
    }
}
