using CommunityToolkit.Mvvm.Messaging;
using Dempbot4.Models.ScriptEngines.Messages;

namespace Dempbot4.Models.ScriptEngines
{
    public class PrintAdapter
    {
        public void Print(params string[] objToPrint)
        {
            foreach (var obj in objToPrint)
            WeakReferenceMessenger.Default.Send(new Console_MSG { Command = obj.ToString() });
        }
    }
}
