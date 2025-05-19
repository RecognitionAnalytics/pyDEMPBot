using MeasureCommons.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeasureCommons.Messages
{

    public class DataAvailable_MSG
    {
        public DataAvailable_MSG(Queue<ChannelDataChunk> data)
        {
            Data = data;
        }
        public Queue<ChannelDataChunk> Data { get; private set; }
    }

    public class StartData_MSG
    {
        public string Mode { get; set; }
        public StartData_MSG()
        {
        }
    }

}
