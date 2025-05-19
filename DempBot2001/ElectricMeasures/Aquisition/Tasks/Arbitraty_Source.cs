using MeasureCommons.Data;
using MeasureCommons.DataChannels;
using NationalInstruments.DAQmx;
using System;
using System.Collections.Generic;

namespace DempBot3.Models.Aquisition.Tasks
{
    public class Arbitraty_Source : DataAquisionTasks
    {
        private double[] function;





        public Arbitraty_Source(string taskName, List<ChannelFunctionEnum> channelFilters,
             double[] samples,
             bool logData = false, string logFile = "") :
            base(taskName, channelFilters, logData, logFile)
        {
            function = samples;


        }

        protected override ChannelDataChunk VetData(ChannelDataChunk dataBlock)
        {
            return dataBlock;
        }

        public override void _StartTask()
        {
            MeasureTimeS = function.Length * OutSampleRate;

            NumberSamples = (int)(Math.Floor(SampleRate * MeasureTimeS));
            in_task.Timing.ConfigureSampleClock("", SampleRate, SampleClockActiveEdge.Rising, SampleQuantityMode.FiniteSamples, NumberSamples);
            out_task.Timing.ConfigureSampleClock("", OutSampleRate, SampleClockActiveEdge.Rising, SampleQuantityMode.FiniteSamples, function.Length);


            _EndVoltage = function[function.Length -1];
            reader = new AnalogMultiChannelReader(in_task.Stream);
            writer = new AnalogSingleChannelWriter(out_task.Stream);


            in_task.Control(TaskAction.Verify);
            out_task.Control(TaskAction.Verify);

            writer.WriteMultiSample(false, function);

            reader.SynchronizeCallbacks = true;
            RunSamples();
        }
    }
}
