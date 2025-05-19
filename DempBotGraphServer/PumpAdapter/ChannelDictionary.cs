using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PumpAdapter
{
    public class ChannelDictionary
    {
        List<double> Times = new List<double>();
        Dictionary<string, List<double>> ChannelHistoryMean = new Dictionary<string, List<double>>();
       

        public static Dictionary<string, double[]> ChannelMean = new Dictionary<string, double[]>();

        DateTime start = DateTime.Now;

        public void Restart()
        {
            ChannelHistoryMean.Clear();
            ChannelMean.Clear();
            Times.Clear();
            start = DateTime.Now;
        }
        public double AddAverage7(string channel, double mean, double std)
        {
            try
            {
                if (channel == null || channel == "")
                    return 0;
                if (ChannelMean.ContainsKey(channel) == false)
                {
                    ChannelMean.Add(channel, new double[] { 0, 0, 0 });
                }
                ChannelMean[channel][0] += (mean);
                ChannelMean[channel][1] += (std);
                ChannelMean[channel][2] += 1;

                return ChannelMean[channel][0] / ChannelMean[channel][2];

            }
            catch (Exception ex)
            {
                return -1;
            }
        }

        public void DataPoint()
        {
            Times.Add(DateTime.Now.Subtract(start).TotalMinutes);
            foreach (var channel in ChannelMean.Keys.ToList())
            {
                if (ChannelHistoryMean.ContainsKey(channel) == false)
                    ChannelHistoryMean.Add(channel, new List<double>());
                ChannelHistoryMean[channel].Add(ChannelMean[channel][0] / (.0001 + ChannelMean[channel][2]));
                ChannelMean[channel] = new double[] { 0, 0, 0 };
            }
        }
        public string[] ChannelNames()
        {
            return ChannelHistoryMean.Keys.ToArray();
        }
        public double[] GetTimes()
        {
            return Times.ToArray();
        }
        public double[] GetChannel(string channel)
        {
            if (channel == null || ChannelHistoryMean[channel].Count == 0)
                return new double[Times.Count];
            else
                return ChannelHistoryMean[channel].ToArray();
        }
        
    }
}
