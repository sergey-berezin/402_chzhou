using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BertViewModel
{
    public interface IErrorSender
    {
        void SendError(string message);
    }

}
