using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeasureCommons.DataChannels
{
    public class ChannelData
    {
        public string Name { get; set; }

        public double[] Samples { get; set; }

        public DateTime StartTime { get; set; }
        public double TimeStep { get; set; }
    }
}
