using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Logger
{
    class LogItem
    {
        public string IpAdress { get; set; }
        
        public string Url { get; set; }
        
        public DateTime startTime { get; set; }

        private Stopwatch timer { get; set; }

        public LogItem(string ip)
        {
            IpAdress = ip;
            timer = new Stopwatch();
            begin();
        }

        private void begin()
        {
            startTime = DateTime.Now;
            timer.Restart();
        }

        private void end()
        {
            timer.Stop();
        }

        public void log()
        {
            Logger.Instance.add(this.ToString());
        }

        public override string ToString()
        {
            end();
            return startTime.ToString("MM/dd/yy H:mm:ss") + " : " + IpAdress + " -> " + Url + " : " + timer.ElapsedMilliseconds + "ms";
        }
    }
}
