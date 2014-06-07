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
    class WebRequest
    {

        private Socket Socket { get; set; }

        public LogItem LogItem { get; set; }


        public WebRequest(Socket socket)
        {
            try
            {

                Socket = socket;
                LogItem = new LogItem(Socket.RemoteEndPoint.ToString());
                Console.WriteLine(socket.RemoteEndPoint);

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
                        Console.WriteLine(sBufferArray[0] + "request");
                        sendFile(sBufferArray);
                        break;
                    case "GET":
                        Console.WriteLine(sBufferArray[0] + "request");
                        sendFile(sBufferArray);
                        break;
                    default:
                        Console.WriteLine(sBufferArray[0] + "request (not supported)");
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

        private void sendFile(string[] sBufferArray)
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

        private void sendError(string message)
        {
            string html = @"<html><head><title>" + message + @"</title></head><body><h1>" + message + @"</h1></body></html>";
            byte[] messageByteArray = Encoding.ASCII.GetBytes(html);
            SendHeader(messageByteArray.Length, message);
            Socket.Send(messageByteArray);
        }

        private string getFile(string path)
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
            WebServer.WebRequests.Release();

        }
    }
}
