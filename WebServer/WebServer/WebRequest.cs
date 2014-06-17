using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Server.Logger;

namespace Server
{
    abstract class WebRequest<T> where T : Server
    {

        protected Socket Socket { get; set; }

        protected LogItem LogItem { get; set; }

        protected T ServerInstance { get; set; }

        public WebRequest(Socket socket, T server)
        {
            ServerInstance = server;
            Socket = socket;
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

                LogItem.Url = Socket.LocalEndPoint + request[1];

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
                if (Socket.Connected)
                {
                    close();
                }
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

        protected void sendRedirect(string url, int statusCode, string statusMessage)
        {
            String header = "HTTP/1.1 " + statusCode + statusMessage +"\r\n";
            header += "Location: " + url + "\r\n";
            Socket.Send(ASCIIEncoding.ASCII.GetBytes(header));
            close();
        }

        protected virtual string getFile(string path)
        {
            if (path == "/" && !ServerInstance.DirBrowsing)
            {
                foreach (string defaultPage in ServerInstance.DefaultPages)
                {
                    FileInfo info = new FileInfo(ServerInstance.WebRoot + "/" + defaultPage);
                    if (info.Exists)
                    {
                        path = "/" + defaultPage;
                        break;
                    }
                }
            }
            return ServerInstance.WebRoot + path;
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

        public virtual void close()
        {
            try
            {
                Socket.Disconnect(false);
                Socket.Dispose();
                LogItem.log();
                Server.WebRequests.Release();
            }
            catch { }

        }

        public void forceClose()
        {
            sendError(423, "Server Reboot");
            close();
        }
    }
}
