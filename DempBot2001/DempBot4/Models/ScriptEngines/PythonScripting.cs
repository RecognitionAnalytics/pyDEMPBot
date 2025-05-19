using CommunityToolkit.Mvvm.Messaging;
using DempBot3.Models.Aquisition;
using Dempbot4.Models.ScriptEngines.Messages;
using Flurl.Http;
using MeasureCommons.Data.Experiments;
using MeasureCommons.Messages;
using Python.Runtime;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Dempbot4.Models.ScriptEngines
{
    public class PythonScripting
    {
        public Action<string> AlertBox { get; set; } = null;
        public void ShowAlert(string message)
        {
            if (AlertBox != null)
                AlertBox(message);
        }
        public void SendNotify(string filename, string wafer, string chip)
        {
            Task.Run(() =>
            {
                try
                {
                    "https://10.212.27.176:7003/RaxDataNotify".PostJsonAsync(new { filename = filename + ".tdms", wafer = wafer, chip = chip })
                  .Wait();
                }
                catch { }
            });
        }

        public void Stop()
        {
            var datarig = (IDataRig)App.Current.Services.GetService(typeof(IDataRig));
            CurrentDisplay.Kill = true;
            datarig.KillCurrentTask();
            Thread.Sleep(600);
            //PythonEngine.Interrupt((ulong) Worker.ManagedThreadId);
            //Thread.Sleep(500);
            //Worker.Abort();
            //Thread.Sleep(500);
            //PythonEngine.Shutdown();



            //StartPython();
        }

        private Dictionary<string, string> DefinedPrograms;

        public string[] GetPrograms()
        {
            return DefinedPrograms.Keys.ToArray();
        }

        public string GetProgram(string program)
        {
            return DefinedPrograms[program];
        }

        public void SaveProgram(string programName, string program)
        {
            program = program.Replace("\t", "    ");
            if (DefinedPrograms.ContainsKey(programName))
                DefinedPrograms.Remove(programName);
            DefinedPrograms.Add(programName, program);
            File.WriteAllText(Path.Combine(App.DataFolder, programName + ".py"), program);

            //if (programName == "Macros")
            //{
            //    var interactivecode = Engine.CreateScriptSourceFromString(program.replace("\t", "    "), SourceCodeKind.AutoDetect);

            //    var parseresult = interactivecode.GetCodeProperties();

            //    try
            //    {
            //        //if (parseresult == ScriptCodeParseResult.Complete || parseresult == ScriptCodeParseResult.Invalid)
            //        interactivecode.Execute(Scope);
            //    }
            //    catch (Exception ex)
            //    {
            //        var errors = Engine.GetService<ExceptionOperations>().FormatException(ex);
            //        WeakReferenceMessenger.Default.Send(new PythonError_MSG { Code = program, Exception = ex, Messsage = errors });
            //    }
            //}
        }


        private DisplayHandler CurrentDisplay = null;
        private void SetupScriptObjects()
        {

            var streamWriter = new StreamWriteEvent();

            Scope.Set("PrintAdapt", (new PrintAdapter()).ToPython());

            string codeToRedirectOutput =
@"import sys
sys.path.insert(0, r'C:\DEMPBot_Settings\DempBotSettings')
from io import StringIO
sys.stdout = mystdout = StringIO()
sys.stdout.flush()
sys.stderr = mystderr = StringIO()
sys.stderr.flush()
print('test')
def printnow(*args):
  PrintAdapt.Print(args)
";

            _RunScript(codeToRedirectOutput);
            CurrentDisplay = (new DisplayHandler());
            Scope.Set("Experiment", this.Experiment.ToPython());
            Scope.Set("Display", CurrentDisplay.ToPython());
            Scope.Set("Intenet", (new InternetHandler()).ToPython());

            ElectronicsProgram Data = (ElectronicsProgram)App.Current.Services.GetService(typeof(ElectronicsProgram));

            Scope.Set("ElectricAdapt", Data.ToPython());

            Data.DataAvailable += Data_DataAvailable;
        }

        private void Data_DataAvailable(Queue<MeasureCommons.Data.ChannelDataChunk> queue)
        {
            WeakReferenceMessenger.Default.Send(new DataAvailable_MSG(queue));
        }

        Experiment Experiment;
        PyModule Scope;
        bool PythonRunning = false;
        private void StartPython()
        {

            

            Worker = new Thread(_ =>
            {
                PythonEngine.Initialize();
                Scope = Py.CreateScope();
                SetupScriptObjects();
                PythonRunning = true;
                GILThread = PythonEngine.BeginAllowThreads();
                while (PythonRunning)
                {
                    if (CodeQueue.TryDequeue(out var code))
                    {
                        try
                        {
                            if (GILThread != IntPtr.Zero)
                                PythonEngine.EndAllowThreads(GILThread);

                            CurrentDisplay.Kill = false;
                            WeakReferenceMessenger.Default.Send(new PlayStatus_MSG { Playing =  PlayStatus.Playing }); 
                            WeakReferenceMessenger.Default.Send(new Console_MSG { Command = "Started" });
                            using (var gil = Py.GIL())
                                _RunScript(code);

                            WeakReferenceMessenger.Default.Send(new PlayStatus_MSG { Playing = PlayStatus.Stopped });
                            WeakReferenceMessenger.Default.Send(new Console_MSG { Command = "Done." });

                            GILThread = PythonEngine.BeginAllowThreads();
                            WeakReferenceMessenger.Default.Send(new PlayDone_MSG());
                        }
                        catch (Exception ex)
                        {
                            WeakReferenceMessenger.Default.Send(new PlayStatus_MSG { Playing = PlayStatus.Error });
                            WeakReferenceMessenger.Default.Send(
                                  new PythonError_MSG
                                  {
                                      Code = "Compile",
                                      Exception = ex,
                                      Messsage = ex.Message + "\n" + ex.StackTrace + "\n"
                                  });
                            break;
                        }
                    }
                    else
                        Thread.Sleep(100);
                }
                if (GILThread != IntPtr.Zero)
                    PythonEngine.EndAllowThreads(GILThread);
                PythonEngine.Shutdown();
            });
            Worker.Start();
        }

        IntPtr GILThread;
        ConcurrentQueue<string> CodeQueue = new ConcurrentQueue<string>();

        public PythonScripting()
        {

            if (File.Exists(@"C:\Python\Python310-32\python310.dll"))
                Runtime.PythonDLL = @"C:\Python\Python310-32\python310.dll";
            else if (File.Exists(@"C:\Python\python310.dll"))
                Runtime.PythonDLL = @"C:\Python\python310.dll";
            else if (File.Exists(@"C:\Python32\python311.dll"))
                Runtime.PythonDLL = @"C:\Python32\python311.dll";
            else if (File.Exists(@"C:\Users\bashc\AppData\Local\Programs\Python\Python310-32\python310.dll"))
                Runtime.PythonDLL = @"C:\Users\bashc\AppData\Local\Programs\Python\Python310-32\python310.dll";


            Experiment = (Experiment)App.Current.Services.GetService(typeof(IExperiment));

            var mFiles = Directory.GetFiles(App.DataFolder, "*.py");
            DefinedPrograms = new Dictionary<string, string>();
            for (int i = 0; i < mFiles.Length; i++)
            {
                var macro = File.ReadAllText(mFiles[i]);
                DefinedPrograms.Add(Path.GetFileNameWithoutExtension(mFiles[i]), macro);
            }

            StartPython();


            WeakReferenceMessenger.Default.Register<Interrupt_MSG>(this, (thisOne, msg) =>
            {
                ((PythonScripting)thisOne).Stop();
            });


            WeakReferenceMessenger.Default.Register<RunScript_MSG>(this, (thisOne, msg) =>
            {
                if (msg.Language == RunLanguages.Python)
                    ((PythonScripting)thisOne).CodeQueue.Enqueue(msg.Command);
            });

            WeakReferenceMessenger.Default.Register<RunCode_MSG>(this, (thisOne, msg) =>
            {
                if (msg.Language == RunLanguages.Python)
                    ((PythonScripting)thisOne).CodeQueue.Enqueue(msg.Code);
            });

            WeakReferenceMessenger.Default.Register<End_MSG>(this, (thisOne, msg) =>
            {
                PythonRunning = false;
                Thread.Sleep(200);
                try
                {
                    Stop();
                }
                catch { }
                Worker.Abort();
            });
        }

        private string _Script;
        public string Script
        {
            get
            {
                return _Script;
            }
            set
            {
                try
                {
                    _Script = value;
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
        }

        public Dictionary<string, string> MethodSignatures = new Dictionary<string, string>();
        private void ExtractMethods(string code)
        {
            foreach (var line in code.Split('\n'))
            {
                var lineT = line.Trim();
                if (lineT.StartsWith("def"))
                {
                    lineT = lineT.Replace(":", "").Replace("def", "").Trim().Replace("self,", "").Replace("self ,", "").Replace("self", "");
                    var lineName = lineT.Split('(')[0].Trim();
                    if (MethodSignatures.ContainsKey(lineName) == false)
                        MethodSignatures.Add(lineName, lineT);
                }
            }
        }

        private Thread Worker;

        public void NotifyProperties()
        {
            if (Scope != null)
            {
                var items = Scope.GetDynamicMemberNames();
                var variableList = new Variable_MSG();
                foreach (var item in items)
                {
                    if (!item.TrimStart().StartsWith("_"))
                    {
                        dynamic itemValue = Scope.Get(item);

                        variableList.Variables.Add(new Tuple<string, object>(item, itemValue));
                    }
                }
                WeakReferenceMessenger.Default.Send<Variable_MSG>(variableList);
            }
        }

        public List<string> GetMethods(string testObject)
        {
            using (var gil = Py.GIL())
            {
                dynamic sys = Py.Import("sys");
                sys.stdout.truncate(0);
                var flush = sys.stdout.seek(0);
                string codeToRedirectOutput =
    @"import inspect
from inspect import signature

members = inspect.getmembers(" + testObject + @")
for member in members:
    if member[0].startswith('_')==False:
        try:
            fun =signature(member[1] )
            print(member[0]+ str(fun)  )
        except:
            print(member[0] )";

                try
                {
                    var result = Scope.Exec(codeToRedirectOutput);
                    string pyStdout = sys.stdout.getvalue(); // Get stdout
                    sys.stdout.flush();
                    var ms = new List<string>();
                    foreach (var method in pyStdout.Split('\n'))
                    {
                        ms.Add(method.Trim());
                    }
                    return ms;
                }
                catch { }
                return null;
            }
        }

        private void _RunScript(string script)
        {
            dynamic sys = Py.Import("sys");
            try
            {
                sys.stdout.truncate(0);
                var flush = sys.stdout.seek(0);
            }
            catch { }

            try
            {
                var result = Scope.Exec(script.Replace("\t", "    "));
                string pyStdout = sys.stdout.getvalue(); // Get stdout
                WeakReferenceMessenger.Default.Send(new Console_MSG { Command = pyStdout.ToString() });
            }
            catch (Exception ex)
            {
                try
                {
                    string pyStderr = sys.stderr.getvalue(); // Get stderr
                    WeakReferenceMessenger.Default.Send(
                        new PythonError_MSG
                        {
                            Code = "Compile",
                            Exception = ex,
                            Messsage = ex.Message + "\n" + ex.StackTrace + "\n" + pyStderr
                        });
                }
                catch (Exception ex2)
                {
                    WeakReferenceMessenger.Default.Send(
                       new PythonError_MSG
                       {
                           Code = "Compile",
                           Exception = ex,
                           Messsage = ex.Message + "\n" + ex.StackTrace + "\n"
                       });
                }
            }
            NotifyProperties();
        }

        public void RunScript()
        {

            CodeQueue.Enqueue(Script);

        }
    }
}
