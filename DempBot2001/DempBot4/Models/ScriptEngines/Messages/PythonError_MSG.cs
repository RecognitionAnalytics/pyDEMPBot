using System;

namespace Dempbot4.Models.ScriptEngines.Messages
{
    internal class PythonError_MSG
    {
        public string Messsage { get; set; }    
        public Exception Exception { get; set; }

        public string Code { get; set; }
    }
}
