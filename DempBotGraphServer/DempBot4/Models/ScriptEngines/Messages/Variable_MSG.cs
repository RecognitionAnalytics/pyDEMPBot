using System;
using System.Collections.Generic;

namespace Dempbot4.Models.ScriptEngines.Messages
{
    public class Variable_MSG
    {
        public List<Tuple<string, object>> Variables { get; set; } = new List<Tuple<string, object>>();
    }

   
}
