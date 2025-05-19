using MeasureCommons.Data;
using MeasureCommons.DataChannels;
using NationalInstruments.DAQmx;
using System;
using System.Collections.Generic;

namespace DempBot3.Models.Aquisition.Tasks
{
    public class RampToVoltage : DataAquisionTasks
    {
        private double PeakAmplitudeV;
        private double RampRate;
        public RampToVoltage(string taskName, List<ChannelFunctionEnum> channelFilters,
             double voltage,    double rampRate_mV_s,          bool logData = false, string logFile = "") :
            base(taskName, channelFilters, logData, logFile, xAxis_is_Time: true)
        {
            PeakAmplitudeV = voltage;
            RampRate = rampRate_mV_s;
            
        }

        protected override ChannelDataChunk VetData(ChannelDataChunk dataBlock)
        {
            return dataBlock;
        }

        public override void _StartTask()
        {
            double lastVoltage = 0;
            if (!double.IsNaN( DataAquisionTasks.LastVoltage))
                lastVoltage = DataAquisionTasks.LastVoltage;

            var rampRate = RampRate / 1000;// V/s
            
            MeasureTimeS = Math.Abs(PeakAmplitudeV - lastVoltage) / rampRate;
            NumberSamples = (int)(Math.Floor(SampleRate * MeasureTimeS));


            if (NumberSamples == 0)
            {
                writer = new AnalogSingleChannelWriter(out_task.Stream);

                _EndVoltage = PeakAmplitudeV;

                if (out_task.AOChannels.Count > 0)
                {
                    out_task.Control(TaskAction.Verify);
                    writer.WriteSingleSample(true, PeakAmplitudeV);
                }

                _EndWrite();
                LastVoltage = PeakAmplitudeV;

            }
            else
            {

                var oSamples = (int)(Math.Floor(OutSampleRate * MeasureTimeS));
                if (oSamples > 1500000)
                {
                    oSamples = 1500000;
                }


                double factor = (PeakAmplitudeV - lastVoltage) / (oSamples - 1+.00000000001);
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
}
