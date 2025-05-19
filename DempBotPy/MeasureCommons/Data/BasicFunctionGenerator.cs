using System;

namespace MeasureCommons.Data
{
    public class BasicFunctionGenerator
    {
        BasicFunctionGeneratorSignal _FunctionType = BasicFunctionGeneratorSignal.Triangle;
        public BasicFunctionGenerator(BasicFunctionGeneratorSignal functionType)
        {
            this._FunctionType = functionType;
        }


        public double Amplitude { get; set; }
        public double Phase { get; set; }
        public double Offset { get; set; }
        public double Frequency { get; set; }
        public double SamplingRate { get; set; }

        private int _NumberOfSamples = 0;
        public int NumberOfSamples
        {
            get
            {
                return _NumberOfSamples;
            }
            set
            {
                _NumberOfSamples = value;
            }
        }
        public double TotalTime
        {
            get
            {
                return SamplingRate * _NumberOfSamples;
            }
            set
            {
                _NumberOfSamples = (int)Math.Ceiling(value / SamplingRate);
            }
        }

        public void Reset()
        {
            Phase = 0;
        }

        public double[] Generate()
        {
            double[] samples = new double[NumberOfSamples];
            switch (this._FunctionType)
            {
                case BasicFunctionGeneratorSignal.Triangle:
                    {
                        for (int i = 0; i < NumberOfSamples; i++)
                        {
                            samples[i] =2* Amplitude/Math.PI * Math.Asin(Math.Cos(Math.PI/2+ 2f * Math.PI * i / SamplingRate * Frequency + Phase)) + Offset;
                        }
                        break;
                    }

                case BasicFunctionGeneratorSignal.Sine:
                    {
                        for (int i = 0; i < NumberOfSamples; i++)
                        {
                            samples[i] = Amplitude * Math.Sin(2f * Math.PI * i / SamplingRate * Frequency + Phase) + Offset;
                        }
                        break;
                    }
                case BasicFunctionGeneratorSignal.Square:
                    {
                        double x = 0;
                        for (int i = 0; i < NumberOfSamples; i++)
                        {
                            x = Math.Sin(2f * Math.PI * i / SamplingRate * Frequency + Phase);
                            samples[i] = (x >= 0 ? Amplitude : -1 * Amplitude) + Offset ;
                        }
                        break;
                    }

            }
           // Phase += NumberOfSamples/ SamplingRate;
            return samples;
        }
    }

    public enum BasicFunctionGeneratorSignal
    {
        Sine, Triangle, Square
    }
}
