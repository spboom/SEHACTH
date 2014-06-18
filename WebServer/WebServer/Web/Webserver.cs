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

        private static List<WebServerRequest> openSockets;

        public WebServer(int port, string root, string[] defaultPages, bool directoryBrowsing)
            : base(port, root, defaultPages, directoryBrowsing)
        {
            if (root.Equals(""))
            {
                WebRoot = @"./Web";
            }

            openSockets = new List<WebServerRequest>(Server.MAXOPENSOCKETS);
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

        public override bool close()
        {
            try
            {
                running = false;
                while (openSockets.Count > 0)
                {
                    openSockets[0].forceClose();
                }
                Listener.Stop();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void EndRequest(WebServerRequest request)
        {
            lock (openSockets)
            {
                openSockets.Remove(request);
            }
        }
    }
}
