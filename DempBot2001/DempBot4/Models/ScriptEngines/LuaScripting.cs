using CommunityToolkit.Mvvm.Messaging;
using DempBot3.Models.Aquisition;
using Dempbot4.Models.ScriptEngines.Messages;
using MeasureCommons.Data.Experiments;
using MeasureCommons.Messages;
using Python.Runtime;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
 

namespace Dempbot4.Models.ScriptEngines
{
    internal class LuaScripting
    {
        public void Stop()
        {
            var datarig = (IDataRig)App.Current.Services.GetService(typeof(IDataRig));

            datarig.KillCurrentTask();
            Thread.Sleep(600);

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
            File.WriteAllText(Path.Combine(App.DataFolder, programName + ".lua"), program);
        }



        private void SetupScriptObjects()
        {


        }

        private void Data_DataAvailable(Queue<MeasureCommons.Data.ChannelDataChunk> queue)
        {
            WeakReferenceMessenger.Default.Send(new DataAvailable_MSG(queue));
        }

        bool PythonRunning = false;
        private Thread Worker;
        private void StartLua()
        {
            Worker = new Thread(_ =>
            {

                SetupScriptObjects();
                PythonRunning = true;

                while (PythonRunning)
                {
                    if (CodeQueue.TryDequeue(out var code))
                    {
                        try
                        {


                            WeakReferenceMessenger.Default.Send(new PlayStatus_MSG { Playing = PlayStatus.Playing });
                            WeakReferenceMessenger.Default.Send(new Console_MSG { Command = "Started" });



                            var AddressFamily = System.Net.IPAddress.Parse("129.219.2.85");

                            IPEndPoint ipEndPoint = new IPEndPoint(AddressFamily, 5025);
                            Socket client = new Socket(
                            ipEndPoint.AddressFamily,
                            SocketType.Stream,
                    ProtocolType.Tcp);

                            client.ConnectAsync(ipEndPoint).Wait();

                            byte[] messageBytes;
                            var response = "";
                            int received;
                            var buffer = new byte[1_024];

                            //string RESET = "\nfor i = 1, 64 do   if (node[i] ~= nil) then     if (node[i].smua ~= nil) then       node[i].smua.source.leveli = 0       node[i].smua.source.levelv = 0     end     if (node[i].smub ~= nil) then       node[i].smub.source.leveli = 0       node[i].smub.source.levelv = 0     end   end end\nfor i = 1, 64 do   if (node[i] ~= nil) then     if (node[i].smua ~= nil) then       node[i].smua.source.output = 0     end     if (node[i].smub ~= nil) then       node[i].smub.source.output = 0     end   end end\nreset()\n";
                            //messageBytes = Encoding.UTF8.GetBytes(RESET);
                            //client.Send(messageBytes, SocketFlags.None);


                            string CLEAR_ERRORS = "\nerrorqueue.clear()\nprint('Environment Reset')";
                            messageBytes = Encoding.UTF8.GetBytes(CLEAR_ERRORS);
                            client.Send(messageBytes, SocketFlags.None);



                            //var message = "me=localnode\n";
                            //message += @"if (me.description) then tmp=me.description .. ' ' else tmp=' ' end
                            //print('[{admin}]' .. me.model .. ',' .. me.revision .. ',' .. me.serialno .. ','  .. tmp .. ',' .. me.linefreq)
                            //";
                            //Debug.Print(message);

                            var lines = code.Split('\n');

                            var dataAdapter = "";
                            foreach (var line in lines)
                            {

                                var dehydrate = line.Replace(" ", "").ToLower();
                                if (dehydrate.StartsWith("--dataadapter"))
                                {
                                    dataAdapter += line + "\n";
                                }
                                else if (dehydrate.StartsWith("--enddataadapter"))
                                {
                                    WeakReferenceMessenger.Default.Send(new LuaGraphAdapter_MSG { Output = dataAdapter });
                                    dataAdapter = "";
                                }
                                else  
                                {
                                    dataAdapter += line + "\n";
                                    //code += line.Substring(0, cCol).Trim() + "\t";
                                }
                                //else
                                //{
                                //  // code += line.Trim() + "\t";
                                //}
                            }

                            messageBytes = Encoding.UTF8.GetBytes("\nloadscript\n");
                            client.Send(messageBytes, SocketFlags.None);

                            foreach (var line in lines)
                            {
                                messageBytes = Encoding.UTF8.GetBytes(line + "\n");
                                client.Send(messageBytes, SocketFlags.None);
                            }

                            messageBytes = Encoding.UTF8.GetBytes("\nendscript\nscript.run()\n");
                            client.Send(messageBytes, SocketFlags.None);

                            messageBytes = Encoding.UTF8.GetBytes("\nprint('@@@Code Finished@@@')\n");
                            client.Send(messageBytes, SocketFlags.None);



                            while (response.Contains("@@@Code Finished@@@") == false)
                            {
                                try
                                {
                                    received = client.Receive(buffer, SocketFlags.None);
                                    response = Encoding.UTF8.GetString(buffer, 0, received).Trim();
                                    Debug.Print(response);

                                    WeakReferenceMessenger.Default.Send(new LuaOutput_MSG { Output = response });

                                }
                                catch (Exception ex)
                                {
                                    WeakReferenceMessenger.Default.Send(new Console_MSG { Command = ex.Message + "\n" + ex.StackTrace });
                                    break;
                                }
                            }
                            messageBytes = Encoding.UTF8.GetBytes("\nprint(string.format('%d', errorqueue.count))\n");
                            client.Send(messageBytes, SocketFlags.None);
                            received = client.Receive(buffer, SocketFlags.None);
                            response = Encoding.UTF8.GetString(buffer, 0, received).Trim();
                            Debug.Print(response);

                            string QUERY_NEXT_ERROR = "\nerrorcode,message,severity,who=errorqueue.next()\nprint(string.format('%d,%s,%s,%s', errorcode, message, severity, who or 'nil'))\n";
                            int nErrors = int.Parse(response);
                            for (int i = 0; i < nErrors; i++)
                            {
                                messageBytes = Encoding.UTF8.GetBytes(QUERY_NEXT_ERROR);
                                client.Send(messageBytes, SocketFlags.None);
                                received = client.Receive(buffer, SocketFlags.None);
                                response = Encoding.UTF8.GetString(buffer, 0, received).Trim();

                                WeakReferenceMessenger.Default.Send(new Console_MSG { Command = response });
                            }

                            client.Shutdown(SocketShutdown.Both);

                            WeakReferenceMessenger.Default.Send(new PlayStatus_MSG { Playing = PlayStatus.Stopped });
                            WeakReferenceMessenger.Default.Send(new Console_MSG { Command = "Done." });
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

            });
            Worker.Start();
        }


        ConcurrentQueue<string> CodeQueue = new ConcurrentQueue<string>();
        Experiment Experiment;
        public LuaScripting()
        {




            Experiment = (Experiment)App.Current.Services.GetService(typeof(IExperiment));

            var mFiles = Directory.GetFiles(App.DataFolder, "*.lua");
            DefinedPrograms = new Dictionary<string, string>();
            for (int i = 0; i < mFiles.Length; i++)
            {
                var macro = File.ReadAllText(mFiles[i]);
                DefinedPrograms.Add(Path.GetFileNameWithoutExtension(mFiles[i]), macro);
            }

            StartLua();


            WeakReferenceMessenger.Default.Register<Interrupt_MSG>(this, (thisOne, msg) =>
            {
                ((LuaScripting)thisOne).Stop();
            });


            WeakReferenceMessenger.Default.Register<RunScript_MSG>(this, (thisOne, msg) =>
            {
                if (msg.Language == RunLanguages.Lua)
                    ((LuaScripting)thisOne).CodeQueue.Enqueue(msg.Command);
            });

            WeakReferenceMessenger.Default.Register<RunCode_MSG>(this, (thisOne, msg) =>
            {
                if (msg.Language == RunLanguages.Lua)
                    ((LuaScripting)thisOne).CodeQueue.Enqueue(msg.Code);
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

                WeakReferenceMessenger.Default.Send(new Console_MSG { Command = "" });
            }
            catch (Exception ex)
            {
                try
                {

                    WeakReferenceMessenger.Default.Send(
                        new PythonError_MSG
                        {
                            Code = "Compile",
                            Exception = ex,
                            Messsage = ex.Message + "\n" + ex.StackTrace + "\n"
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

        }

        public void RunScript()
        {

            CodeQueue.Enqueue(Script);

        }
    }
}
