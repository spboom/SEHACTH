using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Server.Logger;
using Server.Web;

namespace Server
{
    abstract class WebRequest
    {

        protected Socket Socket { get; set; }

        protected LogItem LogItem { get; set; }

        protected Server Server { get; set; }

        public WebRequest(Socket socket, Server server)
        {
            try
            {
                Server = server;
                Socket = socket;
                LogItem = new LogItem(Socket.RemoteEndPoint.ToString());

                //make a byte array and receive data from the client 
                Byte[] bReceive = new Byte[1024];
                int i = socket.Receive(bReceive, bReceive.Length, 0);

                //Convert Byte to String
                string sBuffer = Encoding.ASCII.GetString(bReceive);

                string[] sBufferArray = sBuffer.Split(' ');

                LogItem.Url = Socket.LocalEndPoint + sBufferArray[1];

                switch (sBufferArray[0])
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
            sendFile(sBufferArray[1]);
        }

        protected virtual bool pathInRoot(string path)
        {
            DirectoryInfo rootInfo = new DirectoryInfo(new Uri(Server.WebRoot).LocalPath);
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
                        }
                        SendHeader(messageLength, 200, "OK");
                        Socket.SendFile(filePath, null, null, TransmitFileOptions.Disconnect);
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

        protected void sendError(int statusCode, string statusMessage)
        {
            String completeMessage = statusCode + " " + statusMessage;
            string html = @"<html><head><title>" + statusMessage + @"</title></head><body><h1>" + statusMessage + @"</h1></body></html>";
            sendString(html, statusCode, statusMessage);
        }

        protected void sendString(string message, int statusCode, string statusMessage)
        {
            byte[] messageByteArray = Encoding.ASCII.GetBytes(message);
            SendHeader(messageByteArray.Length, statusCode, statusMessage);
            Socket.Send(messageByteArray);
        }

        public virtual string getFile(string path)
        {
            if (path == "/" && !Server.DirBrowsing)
            {
                foreach (string defaultPage in Server.DefaultPages)
                {
                    FileInfo info = new FileInfo(Server.WebRoot + "/" + defaultPage);
                    if (info.Exists)
                    {
                        path = "/" + defaultPage;
                        break;
                    }
                }
            }
            return Server.WebRoot + path;
        }
        public void SendHeader(int iTotBytes, int statusCode, string statusMessage)
        {
            String sBuffer = "";
            // if Mime type is not provided set default to text/html

            sBuffer = sBuffer + "HTTP/1.1 " + statusCode + " " + statusMessage + "\r\n";
            sBuffer = sBuffer + "Server: My Little Server\r\n";
            sBuffer = sBuffer + "Accept-Ranges: bytes\r\n";
            sBuffer = sBuffer + "Content-Length: " + iTotBytes + "\r\n\r\n";
            Byte[] bSendData = Encoding.ASCII.GetBytes(sBuffer);
            Socket.Send(Encoding.ASCII.GetBytes(sBuffer), Encoding.ASCII.GetBytes(sBuffer).Length, SocketFlags.None);
        }

        public void close()
        {
            Socket.Disconnect(false);
            Socket.Dispose();
            LogItem.log();
            Server.WebRequests.Release();

        }
    }
}
