using NationalInstruments.DAQmx;
using NationalInstruments.Tdms;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Remoting.Channels;
using System.Threading;
using static DempBot3.Models.Aquisition.ElectronicsProgram;

namespace DataControllers
{
    public class ElectricFileAdapter
    {

        public File myTdmsFile;

        public DataFile OpenFile(string filename, Action<string> UserMessages, CancellationToken cancelToken)
        {
            var file = new DataFile();
            UserMessages?.Invoke($"Opening {filename}");
            using (var tdmsFile = new File(filename))
            {
                tdmsFile.Open();

                file.Filename = filename;
                file.Properties = new Dictionary<string, string>();
                bool isIV = false;
                foreach (var prop in tdmsFile.Properties.Keys)
                {
                    var value = tdmsFile.Properties[prop].ToString();
                    if ( value == null) continue;
                     if (prop=="mode" && value == "IV")
                        isIV=true;
                    file.Properties.Add(prop, value);
                }
                if (System.IO. File.Exists( System.IO.Path.GetFileNameWithoutExtension( filename ) ))
                {

                }

                file.Channels = new List<DataChannel>();
                double timeStep = 0;
                long sampleCount = 0;
                foreach (var group in tdmsFile.Groups)
                {
                    foreach (var channel in group.Value.Channels.Keys)
                    {
                        cancelToken.ThrowIfCancellationRequested();
                        UserMessages?.Invoke($"Loading {channel} from {filename}");
                        var deciData = new double[(int)( group.Value.Channels[channel].DataCount/100.0)];
                        var data = group.Value.Channels[channel].GetData<double>().ToArray();
                        int cc = 0;
                        for (int i=0;i<data.Length && cc<deciData.Length;i+=100)
                        {
                                deciData[cc++] = data[i];
                        }

                        file.Channels.Add(new DataChannel { Name = channel, Data = deciData });
                        if (timeStep == 0)
                        {
                            timeStep = (double)group.Value.Channels[channel].Properties["wf_increment"]*100;
                            sampleCount = deciData.Length;
                        }
                    }
                }

                 

                file.Independants = new List<DataChannel>
                {
                    new DataChannel { Name = "Time", Data = Enumerable.Range(0, (int)sampleCount).Select(x => x * timeStep).ToArray() }
                };
                UserMessages?.Invoke($"Done Loading from {filename}");
            }
            return file;
        }
    }

    public class DataFile
    {
        public string Filename { get; set; }
        public Dictionary<string, string> Properties { get; set; }
        public List<DataChannel> Channels { get; set; }

        public List<DataChannel> Independants { get; set; }
    }

    public class DataChannel
    {
        public string Name { get; set; }
        public double[] Data { get; set; }
    }
}
