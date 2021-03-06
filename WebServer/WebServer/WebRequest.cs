﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;
using Server.Logger;
using Server.Control.SessionControl;

namespace Server
{
    abstract class WebRequest
    {

        protected Socket Socket { get; set; }

        protected LogItem LogItem { get; set; }

        protected Server ServerInstance { get; set; }

        protected Session Session { get; set; }
        protected bool newSession;

        protected Dictionary<String, String> Headers { get; set; }

        private bool closed = false;

        public WebRequest(Socket socket, Server server)
        {
            ServerInstance = server;
            Socket = socket;
            Headers = new Dictionary<string, string>();
        }

        public void start()
        {

            try
            {
                LogItem = new LogItem(Socket.RemoteEndPoint.ToString());

                //make a byte array and receive data from the client 
                Byte[] bReceive = new Byte[1024];
                int i = Socket.Receive(bReceive, bReceive.Length, SocketFlags.None);

                //Remove \0 bytes
                List<byte> received = new List<byte>(bReceive);

                received.RemoveAll((byte b) => { return b == '\0'; });


                //Convert Byte to String
                string sBuffer = Encoding.ASCII.GetString(received.ToArray());

                string[] sBufferArray = sBuffer.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

                string[] request = sBufferArray[0].Split(' ');

                for (int j = 1; j < sBufferArray.Length; j++)
                {
                    if (!(request[0] == "POST" && sBufferArray.Length - 1 == j))
                    {
                        string[] parts = sBufferArray[j].Split(new string[] { ": " }, 2, StringSplitOptions.None);
                        Headers.Add(parts[0], parts[1]);
                    }
                }
                LogItem.Url = Socket.LocalEndPoint + request[1];

                Session = ServerInstance.findSession(this, out newSession);


                switch (request[0])
                {
                    case "POST":
                        POST(sBufferArray);
                        break;
                    case "GET":
                        GET(sBufferArray);
                        break;
                    default:
                        sendError(400, "Bad Request");
                        break;
                }
            }
            catch
            { }
            finally
            {
                close();
            }
        }

        protected virtual void POST(string[] sBufferArray)
        {
            send(sBufferArray);

        }
        protected virtual void GET(string[] sBufferArray)
        {
            send(sBufferArray);
        }

        protected virtual void send(string[] sBufferArray)
        {
            sendFile(sBufferArray[0].Split(' ')[1]);
        }

        protected virtual bool pathInRoot(string path)
        {
            DirectoryInfo rootInfo = new DirectoryInfo(new Uri(ServerInstance.WebRoot).LocalPath);
            FileInfo fileInfo = new FileInfo(path);
            DirectoryInfo parent = fileInfo.Directory;
            do
            {
                if (rootInfo.FullName == parent.FullName)
                {
                    return true;
                }
                parent = parent.Parent;
            }
            while (parent != null);

            return false;
        }


        public virtual void sendFile(string path)
        {
            try
            {
                string filePath = getFile(path);

                if (pathInRoot(filePath))
                {
                    FileInfo info = new FileInfo(filePath);
                    int messageLength = 0;
                    if (Socket.Connected)
                    {
                        //todo check mimetype
                        if (info.Exists)
                        {
                            messageLength = (int)info.Length;

                            SendHeader(messageLength, 200, "OK", GetMimeType(info));
                            Socket.SendFile(filePath, null, null, TransmitFileOptions.Disconnect);
                        }
                        else
                        {
                            sendError(404, "File Not Found");
                        }
                    }
                }
                else
                {
                    sendError(403, "Forbidden");
                }
            }
            catch (Exception)
            { }
        }

        public String IP { get { return Socket.RemoteEndPoint.ToString().Split(':')[0]; } }


        protected void sendError(int statusCode, string statusMessage)
        {
            String completeMessage = statusCode + " " + statusMessage;
            string html = @"<html><head><title>" + statusMessage + @"</title></head><body><h1>" + statusMessage + @"</h1></body></html>";
            sendHTMLString(html, statusCode, statusMessage);
        }

        protected void sendString(string message, int statusCode, string statusMessage)
        {
            byte[] messageByteArray = Encoding.ASCII.GetBytes(message);
            SendHeader(messageByteArray.Length, statusCode, statusMessage, "text/plain");
            Socket.Send(messageByteArray);
        }
        protected void sendHTMLString(string message, int statusCode, string statusMessage)
        {
            byte[] messageByteArray = Encoding.ASCII.GetBytes(message);
            SendHeader(messageByteArray.Length, statusCode, statusMessage, null);
            Socket.Send(messageByteArray);
        }
        protected void sendRedirect(string url)
        {
            String header = "HTTP/1.1 301 Redirect\r\n";
            header += "Location: " + url + "\r\n";
            LogItem.responseCode = 301;
            Socket.Send(ASCIIEncoding.ASCII.GetBytes(header));
        }

        protected virtual string getFile(string path)
        {
            DirectoryInfo dInfo = new DirectoryInfo(ServerInstance.WebRoot + path);

            if (dInfo.Exists && !ServerInstance.DirBrowsing)
            {
                foreach (string defaultPage in ServerInstance.DefaultPages)
                {
                    if (path[0] != '/')
                    {
                        path = "/" + path;
                    }
                    if (path[path.Length - 1] != '/')
                    {
                        path = path + "/";
                    }

                    FileInfo info = new FileInfo(ServerInstance.WebRoot + path + defaultPage);
                    if (info.Exists)
                    {
                        path += defaultPage;
                        break;
                    }
                }
            }
            return ServerInstance.WebRoot + path;
        }

        public void SendHeader(int iTotBytes, int statusCode, string statusMessage, string mimeType)
        {
            LogItem.responseCode = statusCode;

            String sBuffer = "";

            if (mimeType == null || mimeType.Length == 0)
            {
                mimeType = "text/html";
            }

            sBuffer += "HTTP/1.1 " + statusCode + " " + statusMessage + "\r\n";
            if (newSession)
            {
                sBuffer += "Set-Cookie: SESSID=" + Session.ID + "\r\n";
            }
            sBuffer += "Server: My Little Server\r\n";
            sBuffer += "Content-Type: " + mimeType + "\r\n";
            sBuffer += "Accept-Ranges: bytes\r\n";
            sBuffer += "Content-Length: " + iTotBytes + "\r\n\r\n";
            Byte[] bSendData = Encoding.ASCII.GetBytes(sBuffer);
            Socket.Send(Encoding.ASCII.GetBytes(sBuffer), Encoding.ASCII.GetBytes(sBuffer).Length, SocketFlags.None);
        }

        public static string GetMimeType(FileInfo fileInfo)
        {
            string mimeType = "application/unknown";
            RegistryKey regKey = Registry.ClassesRoot.OpenSubKey(fileInfo.Extension.ToLower());

            if (regKey != null)
            {
                object contentType = regKey.GetValue("Content Type");
                if (contentType != null)
                    mimeType = contentType.ToString();
            }

            return mimeType;
        }

        public String this[String index]
        {
            get { return Headers.ContainsKey(index) ? Headers[index] : ""; }
        }

        public virtual void close()
        {
            try
            {
                Socket.Disconnect(false);
                Socket.Dispose();
                LogItem.log();
            }
            catch { }
            finally
            {
                if (!closed)
                {
                    closed = true;
                    Server.WebRequests.Release();
                }
            }

        }

        public void forceClose()
        {
            sendError(423, "Server Reboot");
            close();
        }
    }
}
