using CommunityToolkit.Mvvm.Messaging;
using Dempbot4.Models.ScriptEngines.Messages;
using MeasureCommons.Data;
using MeasureCommons.DataChannels;
using MeasureCommons.Messages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows;


namespace Dempbot4.Models.ScriptEngines
{
    public class DisplayHandler
    {

        private class DisplayAdapter
        {
            public string XLabel { get; set; }
            public string YLabel { get; set; }
            public int XCol { get; set; }
            public int YCol { get; set; }
            public string dataStart { get; set; }
            public string dataEnd { get; set; }

            Queue<ChannelDataChunk> channelDataChunks = new Queue<ChannelDataChunk>();
            public string Data { get; set; } ="";
            public DisplayAdapter(string code)
            {
                var lines = code.Split('\n');
                foreach (var line in lines)
                {
                    var parts = line.Split(new string[] { "=", "--" }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 2)
                    {
                        switch (parts[0].ToLower().Trim())
                        {
                            case "xlabel":
                                XLabel = parts[1].Trim();
                                break;
                            case "ylabel":
                                YLabel = parts[1].Trim();
                                break;
                            case "xcol":
                                XCol = int.Parse(parts[1].Trim());
                                break;
                            case "ycol":
                                YCol = int.Parse(parts[1].Trim());
                                break;
                            case "datastart":
                                dataStart = parts[1].Trim();
                                break;
                            case "dataend":
                                dataEnd = parts[1].Trim();
                                break;
                        }
                    }
                }
            }

            List<double> xData = new List<double>();
            List<double> yData = new List<double>();

            private void SaveData(string line )
            {
                var parts = line.Trim().Split(new string[] { "Save", "save" },StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length==2)
                {
                    var fileName = parts[1].Trim();
                    var data =  Data;
                    if (Path.GetDirectoryName(fileName)=="")
                    {
                        fileName = App.DataFolder + "\\" + fileName;
                    }

                    System.IO.File.WriteAllText(fileName, data);
                    WeakReferenceMessenger.Default.Send(new Console_MSG { Command = $"{dataStart} saved to {fileName}" });
                }
                else
                {
                    WeakReferenceMessenger.Default.Send(new Console_MSG { Command = "Save command not understood" });
                }
            }

            public string  AddLine(string line)
            {

                if (line.Contains( dataEnd))
                {
                    if (xData.Count > 0)
                    {
                        SendData();
                       
                    }
                    SaveData(line);

                    xData.Clear();
                    yData.Clear();
                    Data="";
                    return "";
                }

               

                var parts = line.Trim().Split(',');

                if (parts.Length < 2)
                    return dataStart;

                try
                {
                    var xPoint = double.Parse(parts[XCol].Trim());
                    var yPoint = double.Parse(parts[YCol].Trim());

                    xData.Add(xPoint);
                    yData.Add(yPoint);
                    if (xData.Count > 25)
                    {
                        SendData();
                        xData.Clear();
                        yData.Clear();
                    }
                }
                catch { }
                return dataStart;
            }

            private void SendData()
            {
                var xBlock = new List<ChannelData> { new ChannelData { Name = new NamedChannels { Name = XLabel }, StartTime = DateTime.Now, Samples = xData.ToArray() } };
                var DataBlock = new List<ChannelData> { new ChannelData { Name = new NamedChannels { Name = YLabel }, StartTime = DateTime.Now, Samples =yData.ToArray() } };
                channelDataChunks.Enqueue(new ChannelDataChunk()
                {
                    X_Block = xBlock,
                    DataBlock = DataBlock

                });

                WeakReferenceMessenger.Default.Send(new DataAvailable_MSG(channelDataChunks));
            }
        }

        

        string ActiveAdapter = "";

        Dictionary<string, DisplayAdapter> DisplayAdapters = new Dictionary<string, DisplayAdapter>();
        private void ParseAdapter(string adapterString)
        {
            var adapter = new DisplayAdapter(adapterString);
            if (DisplayAdapters.ContainsKey(adapter.dataStart))
                DisplayAdapters.Remove(adapter.dataStart);
            DisplayAdapters.Add(adapter.dataStart, adapter);
            WeakReferenceMessenger.Default.Send(new Console_MSG { Command = "Adapter " + adapter.dataStart + " added" });
        }

        private void ParseLines(string block)
        {
            var lines = block.Split('\n');
            foreach (var line in lines)
            {
                foreach (var key in DisplayAdapters.Keys)
                {
                    if (line.Contains(key))
                    {
                        if (ActiveAdapter != key)
                            WeakReferenceMessenger.Default.Send(new StartData_MSG());
                        ActiveAdapter = key;
                        WeakReferenceMessenger.Default.Send(new Console_MSG { Command = "Adapter " + key + " selected" });
                        break;
                    }
                }
                if (string.IsNullOrEmpty(ActiveAdapter) == false)
                {
                    ActiveAdapter= DisplayAdapters[ActiveAdapter].AddLine(line);
                }
                else
                {
                    WeakReferenceMessenger.Default.Send(new Console_MSG { Command = line });
                }
            }
            if (string.IsNullOrEmpty(ActiveAdapter) == false)
            {
                DisplayAdapters[ActiveAdapter].Data += block;
            }
        }

        public DisplayHandler()
        {
            WeakReferenceMessenger.Default.Register<LuaGraphAdapter_MSG>(this, (thisOne, msg) =>
            {
                ((DisplayHandler)thisOne).ParseAdapter(msg.Output);
            });
            WeakReferenceMessenger.Default.Register<LuaOutput_MSG>(this, (thisOne, msg) =>
            {
                ((DisplayHandler)thisOne).ParseLines(msg.Output);
            });
        }

        public void print(params object[] texts)
        {
            string outText = "";
            for (int i = 0; i < texts.Length; i++)
            {
                outText += " " + texts[i].ToString();
            }
            WeakReferenceMessenger.Default.Send(new Console_MSG { Command = outText });
            WeakReferenceMessenger.Default.Send(new PlayTitle_MSG { Title = outText });
        }

        public void Print(params object[] texts)
        {
            string outText = "";
            for (int i = 0; i < texts.Length; i++)
            {
                outText += " " + texts[i].ToString();
            }
            WeakReferenceMessenger.Default.Send(new Console_MSG { Command = outText });
            WeakReferenceMessenger.Default.Send(new PlayTitle_MSG { Title = outText });
        }

        public void ClearGraphs(string mode)
        {
            WeakReferenceMessenger.Default.Send(new StartData_MSG { Mode = mode });
        }

        public void LabelGraphs(string xLabel, string yLabel)
        {
            WeakReferenceMessenger.Default.Send(new Label_MSG { XLabel = xLabel, YLabel = yLabel });
        }
        public void TitleGraphs(string title)
        {
            WeakReferenceMessenger.Default.Send(new Title_MSG { Title = title });
        }


        public bool Kill = false;

        public void ActiveWait(double seconds)
        {
            var last = DateTime.Now;
            while (Kill == false && last.Subtract(DateTime.Now).TotalMilliseconds / -1000.0 < seconds)
            {
                Thread.Sleep(100);
            }
            if (Kill)
            {
                Kill = false;
                throw new Exception("Program Stopped");
            }
            Kill = false;
        }

        public void ShowAlert(string text)
        {
            MessageBox.Show(text);
        }
    }

    public class Label_MSG
    {
        public string XLabel { get; set; }
        public string YLabel { get; set; }
    }

    public class Title_MSG
    {
        public string Title { get; set; }
    }
}
