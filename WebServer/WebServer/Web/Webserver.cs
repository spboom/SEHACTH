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
    class WebServer
    {

        public static Semaphore WebRequests
        {
            get;
            set;
        }
        static TcpListener server;

        public static string WebRoot
        {
            get;
            private set;
        }

        public static bool DirBrowsing
        {
            get;
            private set;
        }

        public static string[] DefaultPages
        {
            get;
            private set;
        }
        public WebServer(Int32 port, string root, string[] defaultPages, bool directoryBrowsing)
        {
            WebRequests = new Semaphore(20, 20);
            if (root.Equals(""))
            {
                root = @"./Web";
            }
            WebRoot = root;

            DefaultPages = defaultPages;

            DirBrowsing = directoryBrowsing;

            IPAddress ip = IPAddress.Parse("127.0.0.1");

            server = new TcpListener(ip, port);
            server.Start();
            Thread thread = new Thread(new ThreadStart(Print));
            thread.Start();
        }

        private void Print()
        {
            while (true)
            {
                Socket socket = server.AcceptSocket();
                if (socket.Connected)
                {
                    WebRequests.WaitOne();
                    new WebRequest(socket);

                }
            }
        }
    }
}
