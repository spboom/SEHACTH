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
                        sendError("400 Bad Request");
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
            sendFile(sBufferArray);
        }
        protected virtual void GET(string[] sBufferArray)
        {
            sendFile(sBufferArray);
        }


        public virtual void sendFile(string[] sBufferArray)
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

        protected void sendError(string message)
        {
            string html = @"<html><head><title>" + message + @"</title></head><body><h1>" + message + @"</h1></body></html>";
            sendString(html, message);
        }

        protected void sendString(string message, string statusCode)
        {
            byte[] messageByteArray = Encoding.ASCII.GetBytes(message);
            SendHeader(messageByteArray.Length, statusCode);
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
        public void SendHeader(int iTotBytes, string sStatusCode)
        {
            String sBuffer = "";
            // if Mime type is not provided set default to text/html

            sBuffer = sBuffer + "HTTP/1.1 " + sStatusCode + "\r\n";
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
