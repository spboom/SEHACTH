﻿using System;
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

        protected override void GET(string[] sBufferArray)
        {
            send(sBufferArray);
        }

        protected override void send(string[] sBufferArray)
        {
            string path = getFile(sBufferArray[1]);
            bool isDir = Directory.Exists(path);
            if (isDir && Server.DirBrowsing)
            {
                sendFolder(path);
            }
            else if (File.Exists(path))
            {
                sendFile(sBufferArray[1]);
            }
            else
            {
                sendError(404, "Not Found");
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
            sendString(html, 200, "OK");
        }
    }
}
