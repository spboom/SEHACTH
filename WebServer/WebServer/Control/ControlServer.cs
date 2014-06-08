using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Server.Control
{
    class ControlServer : Server
    {
        static readonly string CONTROLROOT = @"./Control";
        static readonly string[] CONTROLDEFAULTPAGES = new string[] { "adminForm.html" };
        static readonly bool CONTROLDIRECTORYBROWSING = false;

        public ControlServer(int port)
            : base(port, CONTROLROOT, CONTROLDEFAULTPAGES, CONTROLDIRECTORYBROWSING)
        { }

        protected override void run()
        {
            while (true)
            {
                Socket socket = Listener.AcceptSocket();
                if (socket.Connected)
                {
                    WebRequests.WaitOne();
                    new Thread(() => new ControlServerRequest(socket, this)).Start();
                }
            }
        }
    }
}
