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
        public WebServerRequest(Socket socket, WebServer server)
            : base(socket, server)
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

        protected override void GET(string[] sBufferArray)
        {
            send(sBufferArray);
        }

        private void send(string[] sBufferArray)
        {
            string path = getFile(sBufferArray[1]);
            bool isDir = Directory.Exists(path);
            if (isDir && Server.DirBrowsing)
            {
                sendFolder(path);
            }
            else if (File.Exists(path))
            {
                sendFile(sBufferArray);
            }
            else
            {
                sendError("404 Not Found");
            }
        }

        private void sendFolder(string path)
        {
            string head, body, html, pathFromRoot = path.Substring(Server.WebRoot.Length);
            if (pathFromRoot.Equals("/"))
            {
                pathFromRoot = "";
            }
            head = @"<head><title>" + pathFromRoot + @"</title></head>";
            body = @"<body><lu>";
            DirectoryInfo dirInfo = new DirectoryInfo(path);
            FileSystemInfo[] fileSystemInfos = dirInfo.EnumerateFileSystemInfos().ToArray();
            foreach (FileSystemInfo info in fileSystemInfos)
            {
                body += @"<li><a href='" + pathFromRoot + "/" + info.Name + @"'>" + info.Name + @"</a></li>";
            }
            body += @"</lu></body>";
            html = @"<html>" + head + body + @"</html>";
            sendString(html, "200 OK");
        }
    }
}
