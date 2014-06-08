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

        public String statusMessage { get; set; }

        public int responseCode { get; set; }

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
            Logger.Instance.addToQueue(this.ToString());
        }

        public override string ToString()
        {
            end();
            return "Timestamp: " + startTime.ToString("MM/dd/yy H:mm:ss") + ", response code: " + responseCode + ", responce time:" + timer.ElapsedMilliseconds + "ms, request: "+ IpAdress + " -> " + Url;
        }
    }
}
