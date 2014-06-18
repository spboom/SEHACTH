using Server.Control.SessionControl;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Server
{
    abstract class Server
    {
        private Dictionary<String, Session> sessions = new Dictionary<string,Session>();
        public static readonly int MAXOPENSOCKETS = 20;
        protected bool running;

        public Server(int port, string root, string[] defaultPages, bool directoryBrowsing = false)
        {
            running = true;
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
                { webRequests = new Semaphore(MAXOPENSOCKETS, MAXOPENSOCKETS); }
                return webRequests;
            }
            set
            {
                webRequests = value;
            }
        }


        public Session FindSession<T>(WebRequest<T> request, out bool newSession) where T:Server
        {
            lock (sessions)
            {
                String id = request["Cookie"];
                if (!String.IsNullOrEmpty(id) && id.Contains("SESSID"))
                {
                    String[] parts = id.Split(new String[1] { ";" }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (String p in parts)
                    {
                        String[] sub = p.Split(new String[1] { "=" }, StringSplitOptions.RemoveEmptyEntries);
                        if (sub.Length == 2 && sub[0].Trim() == "SESSID")
                        {
                            id = sub[1];
                            break;
                        }
                    }

                    //If id is legit and exists
                    if (id.Length == 16 && sessions.ContainsKey(id))
                    {
                        //TODO: maybe log warning
                        if (sessions[id].isTimedout || !sessions[id].IP.Equals(request.IP) || !sessions[id].UserAgent.Equals(request["User-Agent"]))
                        {
                            sessions.Remove(id);
                        }
                        else
                        {
                            sessions[id].ResetTime();
                            newSession = false;
                            return sessions[id];
                        }
                    }
                }
            }

            newSession = false;
            return new Session();
        }

        protected abstract void run();

        protected TcpListener Listener { get; set; }

        private string webRoot;
        public string WebRoot
        {
            get { return webRoot; }
            set
            {
                string path = value;
                if (!Path.IsPathRooted(value))
                {
                    path = Path.Combine(new FileInfo(System.Reflection.Assembly.GetExecutingAssembly().Location).Directory.FullName, value);
                }
                webRoot = Path.GetFullPath(new Uri(path).LocalPath);
            }
        }

        public bool DirBrowsing { get; set; }

        public string[] DefaultPages { get; set; }


        public int Port { get; set; }

        public abstract bool close();
    }
}
