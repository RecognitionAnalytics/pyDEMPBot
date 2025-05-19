namespace MeasureCommons.DataChannels
{
    public class NamedChannels
    {
        public string  Name { get; set; }   
        public string Device_Handle { get; set; }

        public ChannelFunctionEnum ChannelFunction { get; set; }

        public double SampleRate { get; set; }

        public override string ToString()
        {
            return Name + ":" + ChannelFunction.ToString();
        }
    }
}
