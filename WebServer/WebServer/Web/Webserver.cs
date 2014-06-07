using System;
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
        public WebServer(Int32 port, string root, string[] defaultPages, bool directoryBrowsing)
            :base(port, root, defaultPages, directoryBrowsing)
        {
            if (root.Equals(""))
            {
                WebRoot = @"./Web";
            }
        }

        protected override void run()
        {
            while (true)
            {
                Socket socket = Listener.AcceptSocket();
                if (socket.Connected)
                {
                    WebRequests.WaitOne();
                    new WebServerRequest(socket, this);

                }
            }
        }
    }
}
