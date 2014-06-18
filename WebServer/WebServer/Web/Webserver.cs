using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Server.Logger;

namespace Server.Web
{
    class WebServer : Server
    {
        public WebServer(int port, string root, string[] defaultPages, bool directoryBrowsing)
            : base(port, root, defaultPages, directoryBrowsing)
        {
            if (root.Equals(""))
            {
                WebRoot = @"./Web";
            }
        }

        protected override void run()
        {
            while (running)
            {
                try
                {
                    Socket socket = Listener.AcceptSocket();
                    if (socket.Connected)
                    {
                        WebRequests.WaitOne();
                        new Thread(() =>
                        {
                            WebServerRequest request = new WebServerRequest(socket, this);
                            openSockets.Add(request);
                            request.start();
                        }).Start();
                    }
                }
                catch { }
            }
        }
    }
}
