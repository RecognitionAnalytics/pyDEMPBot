using MeasureCommons.Data;
using MeasureCommons.DataChannels;
using NationalInstruments.DAQmx;
using System;
using System.Collections.Generic;

namespace DempBot3.Models.Aquisition.Tasks
{
    public class SigmoidWave_Source : DataAquisionTasks
    {
        private double PeakAmplitudeV;
        private double Climb;

        public SigmoidWave_Source(string taskName, List<ChannelFunctionEnum> channelFilters,
             double voltage, double climb, double measureTimeSec,
             bool logData = false, string logFile = "") :
            base(taskName, channelFilters, logData, logFile, xAxis_is_Time: true)
        {
            PeakAmplitudeV = voltage;
            Climb = climb;
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

            var amplitudeChange = PeakAmplitudeV - lastVoltage;

            var oSamples = MeasureTimeS * OutSampleRate;
            var tSamples = new List<double>();

            for (int i = 0; i < oSamples; i++)
            {
                var ratio = (1 - 1 / (1 + Math.Pow(i * OutSampleRate / Climb, 2.49)));
                if (tSamples.Count > 1500000 || ratio > .95)
                {
                    break;
                }
                tSamples.Add( lastVoltage + amplitudeChange * ratio);
            }

            double[] samples = tSamples.ToArray(); tSamples = null;

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
