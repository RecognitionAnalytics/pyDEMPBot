using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeasureCommons.Messages
{
    public class PlayStatus_MSG
    {
        public PlayStatus Playing
        {
            get; set;
        }
    }

    public enum PlayStatus
    {
        Playing,
        Error,
        Stopped
    }
}
