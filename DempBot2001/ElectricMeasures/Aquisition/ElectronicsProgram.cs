using DataControllers;
using DempBot3.Models.Aquisition.Tasks;
using MeasureCommons.Data;
using MeasureCommons.Data.Experiments;
using MeasureCommons.DataChannels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Remoting.Metadata.W3cXsd2001;

namespace DempBot3.Models.Aquisition
{
    public delegate void DataAvailableEvent(Queue<ChannelDataChunk> queue);
    public class ElectronicsProgram
    {
        // public event DataFinishedEvent DataFinished;
        public event DataStartEvent DataStarted;
        private IDataRig DataRig;
        public ElectronicsProgram()
        {
            DataRig = (IDataRig)LibSettings.DataRig;
        }

        public DataAquisionTasks Zero(bool slowAdjust=true, double rampRate_mV_s = 100)
        {
            if (slowAdjust)
            {
                var rt = new RampToVoltage("zero", new List<ChannelFunctionEnum>()
                        {   ChannelFunctionEnum.BiasVoltage }, voltage: 0, rampRate_mV_s: rampRate_mV_s, logData: false);

                rt.DataRead += Rt_DataRead;
                rt.PersistData = false;
                DataRig.EnqueueTask(rt);
                return rt;
            }
            else
                return SetBias(0);
        }

        public DataAquisionTasks SetBias(double bias)
        {

            ShowData = new Queue<ChannelDataChunk>();
            DataStarted?.Invoke();

            var rt = new ConstantVoltage_Write("SetBias", new List<ChannelFunctionEnum>()
            {  ChannelFunctionEnum.BiasVoltage }, voltage: bias, logData: false);


            //rt.DataRead += Rt_DataRead;
            //rt.DataFinished += curveDataFinished;
            DataRig.EnqueueTask(rt);
            return rt;
        }

        public void SetCustomScale(double slope, double offset)
        {
            DataRig.CustomScale(slope, offset);
        }

        private double OverrideRate = -1;
        public void OverrideSampleRate(double sampleRate)
        {
            OverrideRate = sampleRate;
        }
        private bool _PersistData = false;
        public void PersistData()
        {
            _PersistData = true;
        }

        public DataAquisionTasks RunIVMeasure(string stepName, string activeChannels, double slewRate, double voltage, double cycles)
        {
            DataStarted?.Invoke();

            ShowData = new Queue<ChannelDataChunk>();
            var amp = voltage;
            var offset = 0;

            var freq = 1 / (amp / (slewRate / 1000) * 4);

            string outFile = LibSettings.DataFolder + $"\\current{DateTime.Now.Ticks}.tdms";

            var iv = new TriangleWave_Source(stepName, new List<ChannelFunctionEnum>()
                  { ChannelFunctionEnum.CurrentMonitor, ChannelFunctionEnum.BiasMonitor, ChannelFunctionEnum.BiasVoltage}, amp, freq, offset, cycles / freq,
                  logData: true, logFile: outFile);

            if (!string.IsNullOrEmpty(activeChannels))
                iv.ActiveChannels = activeChannels;

            if (OverrideRate > 0)
            {
                iv.OverrideSampleRate = OverrideRate;
                OverrideRate = -1;
            }
            iv.PersistData = _PersistData;
            _PersistData = false;
            iv.DataRead += Rt_DataRead;
            // iv.DataFinished += curveDataFinished;
            DataRig.EnqueueTask(iv);
            return iv;
        }

        public DataAquisionTasks RunSineMeasure(string stepName, string activeChannels, double frequency, double voltage, double cycles, double offset = 0)
        {
            DataStarted?.Invoke();

            ShowData = new Queue<ChannelDataChunk>();
            var amp = voltage;

            var freq = frequency;

            string outFile = LibSettings.DataFolder + $"\\current{DateTime.Now.Ticks}.tdms";

            var iv = new SineWave_Source(stepName, new List<ChannelFunctionEnum>()
            { ChannelFunctionEnum.CurrentMonitor, ChannelFunctionEnum.BiasMonitor, ChannelFunctionEnum.BiasVoltage}, amp, freq, offset, cycles / freq,
                         logData: true, logFile: outFile);

            if (!string.IsNullOrEmpty(activeChannels))
                iv.ActiveChannels = activeChannels;

            if (OverrideRate > 0)
            {
                iv.OverrideSampleRate = OverrideRate;
                OverrideRate = -1;
            }
            iv.PersistData = _PersistData;
            _PersistData = false;
            iv.DataRead += Rt_DataRead;
            // iv.DataFinished += curveDataFinished;
            DataRig.EnqueueTask(iv);
            return iv;
        }

        public DataAquisionTasks RunArbitrary(string stepName, string activeChannels, double[] samples)
        {
            DataStarted?.Invoke();

            ShowData = new Queue<ChannelDataChunk>();

            string outFile = LibSettings.DataFolder + $"\\current{DateTime.Now.Ticks}.tdms";

            var iv = new Arbitraty_Source(stepName, new List<ChannelFunctionEnum>()
            { ChannelFunctionEnum.CurrentMonitor, ChannelFunctionEnum.BiasMonitor, ChannelFunctionEnum.BiasVoltage}, samples,
                         logData: true, logFile: outFile);

            if (!string.IsNullOrEmpty(activeChannels))
                iv.ActiveChannels = activeChannels;

            if (OverrideRate > 0)
            {
                iv.OverrideSampleRate = OverrideRate;
                OverrideRate = -1;
            }

            iv.DataRead += Rt_DataRead;
            iv.PersistData = _PersistData;
            _PersistData = false;

            DataRig.EnqueueTask(iv);
            return iv;
        }

        public DataAquisionTasks RunRTMeasure(string stepName, string activeChannels, double voltage, double minTime, bool slowStart)
        {
            DataStarted?.Invoke();

            ShowData = new Queue<ChannelDataChunk>();
            string outFile = LibSettings.DataFolder + $"\\current{DateTime.Now.Ticks}.tdms";

            if (slowStart)
            {
                var rt = new RampWave_Source(stepName, new List<ChannelFunctionEnum>()
                        { ChannelFunctionEnum.CurrentMonitor, ChannelFunctionEnum.BiasMonitor, ChannelFunctionEnum.BiasVoltage },
                    voltage: voltage,
                    measureTimeSec: minTime,
                    logData: true,
                    logFile: outFile);

                if (!string.IsNullOrEmpty(activeChannels))
                    rt.ActiveChannels = activeChannels;

                if (OverrideRate > 0)
                {
                    rt.OverrideSampleRate = OverrideRate;
                    OverrideRate = -1;
                }

                rt.DataRead += Rt_DataRead;
                rt.PersistData = _PersistData;
                _PersistData = false;
                DataRig.EnqueueTask(rt);
                return rt;
            }
            else
            {
                var rt = new ConstantVoltage_Read(stepName, new List<ChannelFunctionEnum>()
                    {ChannelFunctionEnum.CurrentMonitor, ChannelFunctionEnum.BiasMonitor, ChannelFunctionEnum.BiasVoltage },
                    voltage: voltage,
                    measureTimeSec: minTime,
                    logData: true,
                    logFile: outFile);

                if (!string.IsNullOrEmpty(activeChannels))
                    rt.ActiveChannels = activeChannels;

                if (OverrideRate > 0)
                {
                    rt.OverrideSampleRate = OverrideRate;
                    OverrideRate = -1;
                }

                rt.DataRead += Rt_DataRead;
                rt.PersistData = _PersistData;
                _PersistData = false;
                DataRig.EnqueueTask(rt);
                return rt;
            }

        }

        public DataAquisionTasks ReadDataPoint(string stepName, string activeChannels)
        {
            DataStarted?.Invoke();

            ShowData = new Queue<ChannelDataChunk>();


            var rt = new VoltageRead(stepName, new List<ChannelFunctionEnum>() { ChannelFunctionEnum.CurrentMonitor, ChannelFunctionEnum.BiasMonitor });
            if (!string.IsNullOrEmpty(activeChannels))
                rt.ActiveChannels = activeChannels;

            rt.DataRead += Rt_DataRead;
            _PersistData = false;
            DataRig.EnqueueTask(rt);
            return rt;
        }

        private Queue<ChannelDataChunk> ShowData = new Queue<ChannelDataChunk>();

        public event DataAvailableEvent DataAvailable;

        public void Rt_DataRead(DataAquisionTasks task)
        {
            var chunk = task.Dequeue();

            if (DataAvailable != null)
                ShowData.Enqueue(chunk);

            while (chunk != null)
            {
                chunk = task.Dequeue();
                if (chunk != null)
                {
                    if (DataAvailable != null)
                        ShowData.Enqueue(chunk);
                }
            }

            if (DataAvailable != null)
                DataAvailable(ShowData);
        }
    }
}
