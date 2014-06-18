using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Control.SessionControl
{
    class Session
    {
        public String ID { get; set; }
        public String IP { get; set; }
        public String UserAgent { get; set; }

        private Dictionary<String, String> data = new Dictionary<string, string>();

        public String this[String index]
        {
            get { return data.ContainsKey(index) ? data[index] : ""; }
            set { if (data.ContainsKey(index)) { data.Remove(index); data.Add(index, value); } }
        }

        private DateTime timestamp = DateTime.Now;
        public void ResetTime() { timestamp = DateTime.Now; }
        public Boolean isTimedout { get { return DateTime.Now.Subtract(timestamp).TotalMinutes > 10; } }
    }
}
