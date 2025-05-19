using MeasureCommons.Data;
using MeasureCommons.DataChannels;
using NationalInstruments.DAQmx;
using System;
using System.Collections.Generic;

namespace DempBot3.Models.Aquisition.Tasks
{
    public class ConstantVoltage_Read : DataAquisionTasks
    {
        
        private double Voltage;
        
        public ConstantVoltage_Read(string taskName, List<ChannelFunctionEnum> channelFilters,
           double voltage, double measureTimeSec,   bool logData = false, string logFile = "") :
          base(taskName, channelFilters,  logData, logFile, xAxis_is_Time:true )
        {
            MeasureTimeS = measureTimeSec;
            Voltage = voltage;
        }
        protected override ChannelDataChunk VetData(ChannelDataChunk dataBlock)
        {
            return dataBlock;
        }
        public override void _StartTask()
        {

            NumberSamples = (int)(Math.Floor(SampleRate * MeasureTimeS));
            
            in_task.Timing.ConfigureSampleClock("", SampleRate, SampleClockActiveEdge.Rising, SampleQuantityMode.ContinuousSamples);

            reader = new AnalogMultiChannelReader(in_task.Stream);
            writer = new AnalogSingleChannelWriter(out_task.Stream);
         

            in_task.Control(TaskAction.Verify);
            //in_task.Control(TaskAction.Commit);

            if (out_task.AOChannels.Count > 0)
            {
                out_task.Control(TaskAction.Verify);
                writer.WriteSingleSample(true, Voltage);
            }
            reader.SynchronizeCallbacks = true;
            
            RunSamples();
        }
    }
}
