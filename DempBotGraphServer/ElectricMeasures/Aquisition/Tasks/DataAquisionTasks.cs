using DataControllers.Aquisition.Files;
using MeasureCommons.Data;
using MeasureCommons.DataChannels;
using NationalInstruments;
using NationalInstruments.DAQmx;
using NationalInstruments.Restricted;
using NationalInstruments.Tdms;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace DempBot3.Models.Aquisition.Tasks
{
    public delegate void DataReadEvent(DataAquisionTasks task);
    public delegate void DataFinishedEvent(string logfile);
    public delegate void DataStartEvent();
    public abstract class DataAquisionTasks : IDisposable
    {
        public bool PersistData { get; set; }
        public event DataReadEvent DataRead;
        public event DataFinishedEvent DataFinished;
        public event DataStartEvent DataStarted;
        protected Task in_task;
        protected Task out_task;
        protected AnalogMultiChannelReader reader;
        protected AnalogSingleChannelWriter writer;
        protected AnalogWaveform<double>[] data;
        protected string LogFile { get; private set; }

        public TempFile TempFile { get; set; }

        protected string TaskName;
        protected bool LogData = false;
        List<ChannelFunctionEnum> ChannelFilters;
        protected double SampleRate = 10000;
        protected double OutSampleRate = 1000;
        protected DateTime QuitTime = DateTime.MinValue;
        public bool xAxisIsTime = false;
        protected double MeasureTimeS;
        protected int NumberSamples = 0;
        public double OverrideSampleRate = -1;

        protected double _EndVoltage = double.NaN;
        public static double LastVoltage = double.NaN;

        public virtual void Dispose()
        {
            if (in_task != null)
            {
                try
                {
                    if (in_task.IsDone == false)
                        in_task.Stop();
                }
                catch { }
                try
                {
                    in_task.Dispose();
                }
                catch { }
            }

            try
            {
                if (out_task.IsDone == false)
                    out_task.Stop();
            }
            catch { }
            try
            {
                out_task.Dispose();
            }
            catch { }
        }

        protected Dictionary<string, NamedChannels> TaskChannels;
        protected IDataRig DataRig;

        public void SetDataRig(IDataRig dataRig)
        {
            DataRig = dataRig;
        }

        private string _ActiveChannels = null;
        public string ActiveChannels
        {
            get { return _ActiveChannels; }
            set { _ActiveChannels = (value == null) ? null : value.ToLower(); }
        }

        protected DataAquisionTasks(string taskName, List<ChannelFunctionEnum> channelFilters,
            bool logData = false, string logFile = "", bool xAxis_is_Time = false)
        {
            FinishedWriting = false;
            LogFile = logFile;
            if (logData && string.IsNullOrEmpty(logFile) == false)
                this.TempFile = new TempFile(logFile);
            TaskName = taskName.Replace(".", "__");
            LogData = logData;
            ChannelFilters = channelFilters;
            this.xAxisIsTime = xAxis_is_Time;
        }

        private string[][] HardChannels = null;
        private double HardSampleRate;
        protected DataAquisionTasks(string taskName, string[][] Channels, double sampleRate,
          bool logData = false, string logFile = "", bool xAxis_is_Time = false)
        {
            FinishedWriting = false;
            LogFile = logFile;
            TaskName = taskName.Replace(".", "__");
            LogData = logData;
            if (logData && string.IsNullOrEmpty(logFile) == false)
                this.TempFile = new TempFile(logFile);
            HardChannels = Channels;
            HardSampleRate = sampleRate;
            this.xAxisIsTime = xAxis_is_Time;
        }



        private Dictionary<string, NamedChannels> FinalizeChannels()
        {
            var selectedChannels = DataRig.SelectedChannels;
            TaskChannels = new Dictionary<string, NamedChannels>();
            foreach (var channel in selectedChannels.Values)
            {
                var safeName = channel.Name.Replace("/", "_");


                if ((channel.ChannelFunction == ChannelFunctionEnum.BiasMonitor ||
                                    channel.ChannelFunction == ChannelFunctionEnum.ReferenceMonitor) &&
                                    ChannelFilters.Contains(channel.ChannelFunction))
                {

                    in_task.AIChannels.CreateVoltageChannel(channel.Device_Handle,
                        safeName,
                        AITerminalConfiguration.Differential,
                        -10,
                        10,
                        AIVoltageUnits.Volts);

                    if (OverrideSampleRate > 0)
                        SampleRate = OverrideSampleRate;
                    else
                        SampleRate = channel.SampleRate;
                    TaskChannels.Add(safeName, channel);
                }
                else if ((channel.ChannelFunction == ChannelFunctionEnum.CurrentMonitor))
                {
                    if (!string.IsNullOrEmpty(ActiveChannels) && ActiveChannels.Contains(safeName.ToLower()) == false)
                        continue;

                    in_task.AIChannels.CreateVoltageChannel(channel.Device_Handle,
                       safeName,
                       AITerminalConfiguration.Differential,
                       -15,
                       15, "VtoA");//
                                   //AIVoltageUnits.Volts);
                    if (OverrideSampleRate > 0)
                        SampleRate = OverrideSampleRate;
                    else
                        SampleRate = channel.SampleRate;


                    TaskChannels.Add(safeName, channel);
                }
                else if ((channel.ChannelFunction == ChannelFunctionEnum.BiasVoltage ||
                   channel.ChannelFunction == ChannelFunctionEnum.ReferenceVoltage) &&
                   ChannelFilters.Contains(channel.ChannelFunction))
                {
                    out_task.AOChannels.CreateVoltageChannel(channel.Device_Handle, safeName,
                        -10,
                        10,
                        AOVoltageUnits.Volts);
                    OutSampleRate = channel.SampleRate;
                    // TaskChannels.Add(safeName, channel);
                }
            }

            return TaskChannels;
        }

        private Dictionary<string, NamedChannels> CreateChannels()
        {
            TaskChannels = new Dictionary<string, NamedChannels>();
            foreach (var channel in HardChannels)
            {
                var deviceName = channel[0];
                var safeName = channel[1];

                in_task.AIChannels.CreateVoltageChannel(deviceName,
                   safeName,
                   AITerminalConfiguration.Differential,
                   -15,
                   15, "VtoA");//
                               //AIVoltageUnits.Volts);

                SampleRate = HardSampleRate;


                TaskChannels.Add(safeName, new NamedChannels
                {
                    ChannelFunction = ChannelFunctionEnum.CurrentMonitor,
                    Device_Handle = deviceName,
                    Name = safeName,
                    SampleRate = HardSampleRate
                });
            }

            return TaskChannels;
        }

        public virtual void _CreateTask()
        {
            in_task = new Task(TaskName + "_in");
            out_task = new Task(TaskName + "_out");


            if (HardChannels == null)
                TaskChannels = FinalizeChannels();
            else
                TaskChannels = CreateChannels();

            if (LogData)
            {
                in_task.ConfigureLogging(LogFile, TdmsLoggingOperation.CreateOrReplace, LoggingMode.LogAndRead, TaskName);
            }
            else
            {
                in_task.Stream.LoggingMode = LoggingMode.Off;
            }
        }

        protected abstract ChannelDataChunk VetData(ChannelDataChunk dataBlock);

        private double startTime = -1;

        private ChannelDataChunk SendData(AnalogWaveform<double>[] data)
        {
            var aquisitionTime = data[0].Timing.TimeStamp;
            var dt = data[0].Timing.SampleInterval.TotalSeconds;
            var datas = new List<AnalogWaveform<double>[]>();
            datas.Add(data);
            AnalogWaveform<double>[] item;
            int nSamples = data[0].SampleCount;

            for (int i = 0; i < DataPile.Count; i++)
            {
                DataPile.TryDequeue(out item);
                datas.Add(item);
                nSamples += item[0].SampleCount;
            }

            var dataBlock = new List<ChannelData>();
            var xBlock = new List<ChannelData>();

            var step = (int)(.001 / dt);
            if (step < 1)
                step = 1;

            for (int i = 0; i < data.Length; i++)
            {
                int cc = 0;
                var decimated = new double[(int)(nSamples / step)];
                var nChannel = TaskChannels[data[i].ChannelName];
                switch (nChannel.ChannelFunction)
                {
                    case ChannelFunctionEnum.ReferenceMonitor:
                    case ChannelFunctionEnum.BiasMonitor:
                        if (xAxisIsTime)
                        {
                            foreach (var d in datas)
                            {
                                var raw = d[i].GetPrecisionTimeStamps();
                                if (startTime == -1)
                                    startTime = raw[0].WholeSeconds + raw[0].FractionalSeconds;
                                for (int j = 0; j < raw.Length && cc < decimated.Length; j += step)
                                {
                                    decimated[cc++] = raw[j].WholeSeconds + raw[j].FractionalSeconds - startTime;
                                }
                            }
                        }
                        else
                        {
                            foreach (var d in datas)
                            {
                                var raw = d[i].GetScaledData();
                                for (int j = 0; j < raw.Length && cc < decimated.Length; j += step)
                                {
                                    decimated[cc++] = raw[j];
                                }
                            }

                        }
                        xBlock.Add(new ChannelData { Name = nChannel, Samples = decimated, StartTime = aquisitionTime, TimeStep = dt });
                        break;
                    default:
                        foreach (var d in datas)
                        {
                            var raw = d[i].GetScaledData();
                            for (int j = 0; j < raw.Length && cc < decimated.Length; j += step)
                            {
                                decimated[cc++] = raw[j];
                            }
                        }
                        dataBlock.Add(new ChannelData { Name = nChannel, Samples = decimated, StartTime = aquisitionTime, TimeStep = dt });
                        break;
                }
            }
            var chunk = VetData(new ChannelDataChunk { X_Block = xBlock, DataBlock = dataBlock });

            if (PersistData)
            {
                DataPersist.Add(chunk);
            }

            return chunk;
        }

        private List<ChannelDataChunk> DataPersist = new List<ChannelDataChunk>();

        public ChannelDataChunk[] GetData()
        {
            var dp = DataPersist.ToArray();
            DataPersist = new List<ChannelDataChunk>();
            return dp;
        }

        public ChannelDataChunk Dequeue()
        {
            if (DataPile.Count > 0)
            {
                DataPile.TryDequeue(out var dataChunk);
                return SendData(dataChunk);
            }
            else
                return null;
        }

        ConcurrentQueue<AnalogWaveform<double>[]> DataPile = new ConcurrentQueue<AnalogWaveform<double>[]>();


        /// These are used no matter what the lexer says
        Thread Reader, Writer;

        public void ForceStop()
        {
            _ForceFinish = true;
            NumberSamples = int.MaxValue;
            _FinishedWriting = true;
            Thread.Sleep(200);

            try
            {
                in_task.Stop();
                in_task.Dispose();
            }
            catch { }
            try
            {
                out_task.Stop();
                out_task.Dispose();
            }
            catch { }

        }

        private bool _FinishedWriting = false;

        private bool _ForceFinish = false;
        public bool FinishedWriting
        {
            set { _FinishedWriting = value; }
            get
            {
                if (_ForceFinish)
                {
                    throw new Exception("Program Stopped");
                }
                return _FinishedWriting;
            }
        }

        private void ReadWork()
        {
            Debug.Print("Start Recording");
            DataStarted?.Invoke();
            FinishedWriting = false;
            QuitTime = DateTime.Now.AddSeconds(MeasureTimeS);
            NumberSamples -= 1;
            in_task.Start();
            out_task.Start();

            int buffSize = 10000;
            int samplesRead = 0;
            while (NumberSamples > 0)
            {
                try
                {

                    AnalogWaveform<double>[] buffer = new AnalogWaveform<double>[TaskChannels.Count];
                    for (int i = 0; i < buffer.Length; i++)
                        buffer[i] = new AnalogWaveform<double>(buffSize, buffSize);

                    buffer = reader.MemoryOptimizedReadWaveform(buffSize, buffer);
                    samplesRead = buffer[0].SampleCount;

                    if (samplesRead < buffSize)
                    {
                        Debug.Print("");
                    }
                    NumberSamples -= samplesRead;
                    DataPile.Enqueue(buffer);
                }
                catch (Exception ex)
                {
                    Debug.Print(ex.ToString());
                    break;
                }
            }
            DataAquisionTasks.LastVoltage = _EndVoltage;
            _EndTask();
            FinishedWriting = true;
        }

        protected void RunSamples()
        {
            Reader = new Thread(ReadWork);
            Reader.Priority = ThreadPriority.Highest;

            Writer = new Thread(() =>
            {
                var update = DateTime.Now;
                while (!_FinishedWriting)
                {
                    Thread.Sleep(100);
                    if (!_FinishedWriting && DateTime.Now.Subtract(update).TotalMilliseconds > 1000)
                    {
                        if (DataRead != null && DataPile.Count > 0)
                            DataRead(this);
                        update = DateTime.Now;
                    }
                }
                DataRead?.Invoke(this);
                DataFinished(LogFile);
            });


            Reader.Start();
            Writer.Start();
        }

        protected void RunOutput()
        {

            //Writer = System.Threading.Tasks.Task.Run(() =>
            //{
            //    while (DateTime.Now < QuitTime)
            //    {
            //        Thread.Sleep(1000);

            //    }
            //    _EndTask(true);
            //    DataFinished(LogFile);
            //});
        }


        public abstract void _StartTask();


        protected void _EndWrite()
        {
            _EndTask();
            FinishedWriting = true;
            DataFinished(LogFile);

        }
        public virtual void _EndTask(bool immediateStop = false, int waitTimeMS = 1000)
        {
            if (immediateStop)
            {
                if (in_task != null)
                    in_task.Stop();
                if (out_task != null)
                    out_task.Stop();
            }
            else
            {
                try
                {

                    in_task.Control(TaskAction.Abort);

                    if (in_task != null)
                        in_task.Stop();

                    //if (in_task != null)
                    //    in_task.WaitUntilDone(waitTimeMS);
                }
                catch
                {
                    try
                    {
                        if (in_task != null)
                            in_task.Stop();

                        //if (in_task != null)
                        //    in_task.WaitUntilDone(waitTimeMS);
                    }
                    catch
                    {

                    }
                }
                try
                {

                    out_task.Control(TaskAction.Abort);
                    if (out_task != null)
                        out_task.Stop();
                    //if (out_task != null)
                    //    out_task.WaitUntilDone(waitTimeMS);
                }
                catch
                {
                    try
                    {
                        if (out_task != null)
                            out_task.Stop();
                        //if (out_task != null)
                        //    out_task.WaitUntilDone(waitTimeMS);
                    }
                    catch
                    {

                    }
                }
            }

            Thread.Sleep((int)(.1 * 1000));
        }
    }
}
