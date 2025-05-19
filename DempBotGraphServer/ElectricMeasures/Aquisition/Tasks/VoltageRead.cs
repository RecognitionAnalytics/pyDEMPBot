using MeasureCommons.Data;
using MeasureCommons.DataChannels;
using NationalInstruments.DAQmx;
using System;
using System.Collections.Generic;

namespace DempBot3.Models.Aquisition.Tasks
{
    public class VoltageRead : DataAquisionTasks
    {
        public VoltageRead(string taskName, List<ChannelFunctionEnum> channelFilters ) :
          base(taskName, channelFilters, false ,"", xAxis_is_Time: true)
        {


        }

        public VoltageRead(string taskName, string[][] channelFilters ) :
          base(taskName, channelFilters, 1000, false, "", xAxis_is_Time: true)
        {


        }

        protected override ChannelDataChunk VetData(ChannelDataChunk dataBlock)
        {
            return dataBlock;
        }
        public override void _StartTask()
        {
            in_task.Timing.ConfigureSampleClock("", SampleRate, SampleClockActiveEdge.Rising, SampleQuantityMode.FiniteSamples, 2);
            
            reader = new AnalogMultiChannelReader(in_task.Stream);


            in_task.Control(TaskAction.Verify);
            Samples = reader.ReadSingleSample();

           
            _EndTask();
            FinishedWriting = true;
        }

        public double[] Samples = null;
    }
}
