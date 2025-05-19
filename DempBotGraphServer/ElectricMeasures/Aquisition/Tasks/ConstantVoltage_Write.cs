using MeasureCommons.Data;
using MeasureCommons.DataChannels;
using NationalInstruments.DAQmx;
using System;
using System.Collections.Generic;

namespace DempBot3.Models.Aquisition.Tasks
{
    public class ConstantVoltage_Write : DataAquisionTasks
    {
        private double Voltage;

        public ConstantVoltage_Write(string taskName, List<ChannelFunctionEnum> channelFilters,
                                     double voltage, bool logData = false, string logFile = ""):
           base(taskName, channelFilters, logData, logFile, xAxis_is_Time: true)
        {
            MeasureTimeS = 0;
            Voltage = voltage;
        }

        protected override ChannelDataChunk VetData(ChannelDataChunk dataBlock)
        {
            return dataBlock;
        }

        public override void _StartTask()
        {
            NumberSamples = (int)(Math.Floor(SampleRate * MeasureTimeS));
            writer = new AnalogSingleChannelWriter(out_task.Stream);

            _EndVoltage = Voltage;

            if (out_task.AOChannels.Count > 0)
            {
                out_task.Control(TaskAction.Verify);
                writer.WriteSingleSample(true, Voltage);
            }

            _EndWrite();
            LastVoltage = Voltage;
        }
    }
}
