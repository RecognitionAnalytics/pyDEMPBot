using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeasureCommons.DataChannels
{
    public enum ChannelFunctionEnum:int
    {
        CurrentMonitor=0, BiasMonitor=1, ReferenceMonitor=6,
        BiasVoltage = 2, ReferenceVoltage = 3, Other=4,OtherVoltage=5
    }
}
