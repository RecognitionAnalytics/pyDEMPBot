using Flurl.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dempbot4.Models.ScriptEngines
{
    public class InternetHandler
    {
        public void SendNotify(string filename, string wafer, string chip)
        {
            Task.Run(() =>
            {
                try
                {
                    "https://10.212.27.176:7003/RaxDataNotify".PostJsonAsync(new { filename = filename + ".tdms", wafer = wafer, chip = chip })
                  .Wait();
                }
                catch { }
            });
        }
    }
}
