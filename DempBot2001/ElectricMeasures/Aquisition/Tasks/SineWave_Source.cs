using MeasureCommons.Data;
using MeasureCommons.DataChannels;
using NationalInstruments.DAQmx;
using System;
using System.Collections.Generic;

namespace DempBot3.Models.Aquisition.Tasks
{
    public class SineWave_Source : DataAquisionTasks
    {
        private double PeakAmplitudeV;
        private double Frequency;
        private double OffsetV;
        
         
        BasicFunctionGenerator FunctionGenerator;

        public SineWave_Source(string taskName, List<ChannelFunctionEnum> channelFilters,
             double peakAmplitudeV, double frequency, double offsetV, double measureTimeSec ,  
             bool logData = false, string logFile = "" ) :
            base(taskName, channelFilters,   logData, logFile)
        {
            PeakAmplitudeV = peakAmplitudeV;
            Frequency = frequency;
            OffsetV = offsetV;
           
            FunctionGenerator = new BasicFunctionGenerator(BasicFunctionGeneratorSignal.Sine);
            MeasureTimeS = measureTimeSec;
        }

        protected override ChannelDataChunk VetData(ChannelDataChunk dataBlock)
        {
            return dataBlock;
        }

        public override void _StartTask()
        {
            
            FunctionGenerator.Amplitude = PeakAmplitudeV;
            FunctionGenerator.Phase = Math.PI/4;
            FunctionGenerator.Frequency = Frequency;
            FunctionGenerator.SamplingRate = OutSampleRate;
            FunctionGenerator.Offset = OffsetV;
            
            FunctionGenerator.NumberOfSamples = (int)(Math.Floor(OutSampleRate * MeasureTimeS));
            FunctionGenerator.Reset();

            NumberSamples = (int)(Math.Floor(SampleRate * MeasureTimeS));
            in_task.Timing.ConfigureSampleClock("", SampleRate, SampleClockActiveEdge.Rising, SampleQuantityMode.ContinuousSamples);
            out_task.Timing.ConfigureSampleClock("", OutSampleRate, SampleClockActiveEdge.Rising, SampleQuantityMode.FiniteSamples, FunctionGenerator.NumberOfSamples);


            reader = new AnalogMultiChannelReader(in_task.Stream);
            writer = new AnalogSingleChannelWriter(out_task.Stream);

            in_task.Control(TaskAction.Verify);
            out_task.Control(TaskAction.Verify);

            var samples = FunctionGenerator.Generate();

            _EndVoltage = samples[samples.Length - 1];

            writer.WriteMultiSample(false, samples);
            reader.SynchronizeCallbacks = true;
            RunSamples();

            //reader.BeginReadWaveform(500, callBack, in_task);
        }
    }
}
