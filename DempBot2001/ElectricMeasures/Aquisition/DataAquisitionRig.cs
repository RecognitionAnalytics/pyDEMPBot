using DataControllers;
using DempBot3.Models.Aquisition.Tasks;
using MeasureCommons.DataChannels;
using NationalInstruments.DAQmx;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;


namespace DempBot3.Models.Aquisition
{
    public class DataAquisitionRig : IDataRig
    {




        LinearScale myCustomScale;
        public void CustomScale(double slope, double offset)
        {
            myCustomScale = new LinearScale("VtoA", slope, offset);
        }
        public Dictionary<string, NamedChannels> SelectedChannels { get; set; }

        public ConcurrentQueue<DataAquisionTasks> TaskQueue = new ConcurrentQueue<DataAquisionTasks>();
        DataAquisionTasks CurrentTask;
        public void EnqueueTask(DataAquisionTasks dataTask)
        {
            dataTask.SetDataRig(this);
            if (TaskQueue.Count > 0 || CurrentTask != null)
            {
                TaskQueue.Enqueue(dataTask);
            }
            else
            {
                CurrentTask = dataTask;
                StartTask();
            }
        }

        public void KillCurrentTask()
        {
            try
            {
                if (CurrentTask != null)
                    CurrentTask.ForceStop();
            }
            catch { }
        }

        private void StartTask()
        {
            CurrentTask._CreateTask();
            CurrentTask.DataFinished += DataTask_DataFinished;
            CurrentTask._StartTask();
        }

        private void DataTask_DataFinished(string logfile)
        {
            CurrentTask.Dispose();
            CurrentTask = null;
            if (TaskQueue.Count > 0)
            {
                TaskQueue.TryDequeue(out CurrentTask);
                StartTask();
            }
        }

        public void Save(Dictionary<string, NamedChannels> savedChannels)
        {
            SelectedChannels = savedChannels;
            var settingsFile = Path.Combine(LibSettings.DataFolder, "Channels.json");

            var channelPlan = JsonConvert.SerializeObject(savedChannels);
            File.WriteAllText(settingsFile, channelPlan);
        }

        public List<string> DeviceMonitorChannels()
        {
            var deviceChannels = new List<string>();
            foreach (var channel in DaqSystem.Local.GetPhysicalChannels(PhysicalChannelTypes.AI, PhysicalChannelAccess.External))
            {
                deviceChannels.Add(channel);
            }
            foreach (var channel in DaqSystem.Local.GetPhysicalChannels(PhysicalChannelTypes.AI, PhysicalChannelAccess.Internal))
            {
                deviceChannels.Add(channel);
            }
            return deviceChannels;
        }

        public List<string> DeviceOutputChannels()
        {
            var deviceChannels = new List<string>();
            foreach (var channel in DaqSystem.Local.GetPhysicalChannels(PhysicalChannelTypes.AO, PhysicalChannelAccess.External))
            {
                deviceChannels.Add(channel);
            }
            return deviceChannels;
        }

        public static DateTime StartExperiment = DateTime.Now;
        public DataAquisitionRig()
        {
            var settingsFile = Path.Combine(LibSettings.DataFolder, "Channels.json");
            if (File.Exists(settingsFile))
            {
                SelectedChannels = JsonConvert.DeserializeObject<Dictionary<string, NamedChannels>>(File.ReadAllText(settingsFile));
            }

            try
            {
                CustomScale(-1 / .48, 0);
            }
            catch { }
        }


      public void LoadSpecificChannels(string filename)
        {
            
            if (File.Exists(filename))
            {
                SelectedChannels = JsonConvert.DeserializeObject<Dictionary<string, NamedChannels>>(File.ReadAllText(filename));
            }

        }
    }

    public interface IDataRig
    {
        void CustomScale(double slope, double offset);
        void EnqueueTask(DataAquisionTasks dataTask);
        void Save(Dictionary<string, NamedChannels> savedChannels);

        void LoadSpecificChannels(string filename);
        void KillCurrentTask();

        Dictionary<string, NamedChannels> SelectedChannels { get; set; }
        List<string> DeviceMonitorChannels();
        List<string> DeviceOutputChannels();
    }
}
