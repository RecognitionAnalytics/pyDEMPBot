namespace Dempbot4.Models.ScriptEngines.Messages
{
    public class RunScript_MSG
    {
        public RunLanguages Language { get; set; }
        public string Command { get; set; }
    }

    public enum RunLanguages
    {
              Python,
        Lua
    }
}
