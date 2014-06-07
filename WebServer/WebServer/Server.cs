using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Server
{
    abstract class Server
    {

        public Server(int port, string root, string[] defaultPages, bool directoryBrowsing = false)
        {
            Port = port;
            WebRoot = root;
            DefaultPages = defaultPages;
            DirBrowsing = directoryBrowsing;

            IPAddress ip = IPAddress.Parse("127.0.0.1");
            Listener = new TcpListener(ip, port);
            Listener.Start();

            new Thread(run).Start();
        }

        private static Semaphore webRequests;
        public static Semaphore WebRequests
        {
            get
            {
                if (webRequests == null)
                { webRequests = new Semaphore(20, 20); }
                return webRequests;
            }
            set
            {
                webRequests = value;
            }
        }

        protected abstract void run();

        protected TcpListener Listener { get; set; }

        public string WebRoot { get; protected set; }

        public bool DirBrowsing { get; private set; }

        public string[] DefaultPages { get; private set; }


        public int Port { get; set; }
    }
}
