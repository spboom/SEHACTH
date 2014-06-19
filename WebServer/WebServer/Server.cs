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
        public Dictionary<String, Session> sessions = new Dictionary<string, Session>();
        public static readonly int MAXOPENSOCKETS = 20;
        protected bool running;
        protected List<WebRequest> openSockets;

        public Server(int port, string root, string[] defaultPages, bool directoryBrowsing = false)
        {
            openSockets = new List<WebRequest>(Server.MAXOPENSOCKETS);

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
                {
                    webRequests = new Semaphore(MAXOPENSOCKETS, MAXOPENSOCKETS);
                }
                return webRequests;
            }
            private set
            {
                webRequests = value;
            }
        }


        public Session findSession(WebRequest request, out bool newSession)
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

                Session session = new Session();
                do { session.ID = Authentication.randomString(16); } while (sessions.ContainsKey(session.ID));
                session.IP = request.IP;
                session.UserAgent = request["User-Agent"];
                sessions.Add(session.ID, session);

                newSession = true;
                return session;
            }
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

        public bool close()
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


        public void EndRequest(WebRequest request)
        {
            lock (openSockets)
            {
                openSockets.Remove(request);
            }
        }

        public void removeSession(Session session)
        {
            lock (sessions)
            {
                sessions.Remove(session.ID);
            }
        }
    }
}
