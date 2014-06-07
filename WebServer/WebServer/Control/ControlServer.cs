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
        public ControlServer(Int32 port)
            : base(port, @"./Control", new string[] { "adminForm.html" }, false)
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
