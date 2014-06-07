using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Server.Logger;

namespace Server.Web
{
    class WebServerRequest : WebRequest
    {
        public WebServerRequest(Socket socket)
            : base(socket)
        { }

        public override void sendFile(string[] sBufferArray)
        {
            String filePath = getFile(sBufferArray[1]);

            try
            {
                FileInfo info = new FileInfo(filePath);
                int messageLength = 0;
                if (Socket.Connected)
                {
                    //todo check mimetype
                    if (info.Exists)
                    {
                        messageLength = (int)info.Length;
                    }
                    SendHeader(messageLength, "200 OK");
                    Socket.SendFile(filePath, null, null, TransmitFileOptions.Disconnect);
                }
            }
            catch (Exception)
            { }
        }

        private override void GET(string[] sBufferArray)
        {
            send(sBufferArray);
        }

        private void send(string[] sBufferArray)
        {
            string path = getFile(sBufferArray[1]);
            bool isDir = Directory.Exists(path);
            if (isDir && WebServer.DirBrowsing)
            {
                sendFolder(path);
            }
            else if (File.Exists(path))
            {
                sendFile(sBufferArray);
            }
                sendError("404 Not Found");
        }

        public override string getFile(string path)
        {
            if (path == "/")
            {
                foreach (string defaultPage in WebServer.DefaultPages)
                {
                    FileInfo info = new FileInfo(WebServer.WebRoot + "/" + defaultPage);
                    if (info.Exists)
                    {
                        path = "/" + defaultPage;
                        break;
                    }
                }
            }
            return WebServer.WebRoot + path;
        }

        private void sendFolder(string sBufferArray)
        {
            throw new NotImplementedException();
        }
    }
}
