using MeasureCommons.Data;
using MeasureCommons.DataChannels;
using NationalInstruments.DAQmx;
using System;
using System.Collections.Generic;

namespace DempBot3.Models.Aquisition.Tasks
{
    public class RampWave_Source : DataAquisionTasks
    {
        private double PeakAmplitudeV;
        
        public RampWave_Source(string taskName, List<ChannelFunctionEnum> channelFilters,
             double voltage,  double measureTimeSec,
             bool logData = false, string logFile = "") :
            base(taskName, channelFilters, logData, logFile, xAxis_is_Time: true)
        {
            PeakAmplitudeV = voltage;
          
            MeasureTimeS = measureTimeSec;
        }

        protected override ChannelDataChunk VetData(ChannelDataChunk dataBlock)
        {
            return dataBlock;
        }

        public override void _StartTask()
        {

            NumberSamples = (int)(Math.Floor(SampleRate * MeasureTimeS));

            double lastVoltage = 0;
            if (!double.IsNaN(DataAquisionTasks.LastVoltage))
                lastVoltage = DataAquisionTasks.LastVoltage;


            var rampRate = 50.0 / 1000;// V/s
            var rampTime = Math.Abs(PeakAmplitudeV - lastVoltage) / rampRate;


            var oSamples = (int)(Math.Floor(OutSampleRate * rampTime));
            if (oSamples > 1500000)
            {
                oSamples = 1500000;
            }

            var factor = (PeakAmplitudeV - lastVoltage) / (oSamples-1 + .00000000001);

            double[] samples = new double[oSamples];
            for (int i = 0; i < samples.Length; i++)
            {
                samples[i] = i * factor + lastVoltage;
            }

            _EndVoltage = PeakAmplitudeV;
            in_task.Timing.ConfigureSampleClock("", SampleRate, SampleClockActiveEdge.Rising, SampleQuantityMode.ContinuousSamples);
            out_task.Timing.ConfigureSampleClock("", OutSampleRate, SampleClockActiveEdge.Rising, SampleQuantityMode.FiniteSamples, samples.Length);

            reader = new AnalogMultiChannelReader(in_task.Stream);
            writer = new AnalogSingleChannelWriter(out_task.Stream);

            out_task.Control(TaskAction.Verify);
            in_task.Control(TaskAction.Verify);

            writer.WriteMultiSample(false, samples);
            
            RunSamples();
        }
    }
}
