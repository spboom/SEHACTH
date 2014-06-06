using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Server.Web
{
    class WebServer
    {
        TcpListener server;
        string webRoot;
        bool dirBrowsing;
        string[] defaultPages;
        public WebServer(Int32 port, string root, string[] defaultPages, bool directoryBrowsing)
        {

            if (root.Equals(""))
            {
                root = @"./Web";
            }
            webRoot = root;

            this.defaultPages = defaultPages;

            dirBrowsing = directoryBrowsing;

            IPAddress ip = IPAddress.Parse("127.0.0.1");

            server = new TcpListener(ip, port);
            server.Start();
            Thread thread = new Thread(new ThreadStart(Print));
            thread.Start();
        }

        private void Print()
        {
            while (true)
            {
                Socket socket = server.AcceptSocket();
                if (socket.Connected)
                {
                    Console.WriteLine(socket.RemoteEndPoint);

                    //make a byte array and receive data from the client 
                    Byte[] bReceive = new Byte[1024];
                    int i = socket.Receive(bReceive, bReceive.Length, 0);

                    //Convert Byte to String
                    string sBuffer = Encoding.ASCII.GetString(bReceive);

                    ////At present we will only deal with GET type
                    //if (sBuffer.Substring(0, 3) != "GET")
                    //{
                    //    Console.WriteLine("Only Get Method is supported..");
                    //    socket.Close();
                    //    return;
                    //}

                    string[] sBufferArray = sBuffer.Split(' ');

                    switch (sBufferArray[0])
                    {
                        case "POST":
                            Console.WriteLine(sBufferArray[0] + "request");
                            break;
                        case "GET":
                            Console.WriteLine(sBufferArray[0] + "request");
                            sendFile(sBufferArray, ref socket);
                            break;
                        default:
                            Console.WriteLine(sBufferArray[0] + "request (not supported)");
                            sendError("400 Bad Request", ref socket);
                            break;
                    }
                    socket.Close();
                }
            }
        }

        private void sendFile(string[] sBufferArray, ref Socket socket)
        {
            String filePath = getFile(sBufferArray[1]);

            try
            {
                FileInfo info = new FileInfo(filePath);
                int messageLength = 0;
                if (socket.Connected)
                {
                    //todo check mimetype
                    if (info.Exists)
                    {
                        messageLength = (int)info.Length;
                    }
                    SendHeader(messageLength, "200 OK", ref socket);
                    socket.SendFile(filePath, null, null, TransmitFileOptions.Disconnect);
                }
            }
            catch (Exception)
            { }
        }

        private void sendError(string message, ref Socket socket)
        {
            string html = @"<html><head><title>" + message + @"</title></head><body><h1>" + message + @"</h1></body></html>";
            byte[] messageByteArray = Encoding.ASCII.GetBytes(html);
            SendHeader(messageByteArray.Length, message, ref socket);
            socket.Send(messageByteArray);
        }

        private string getFile(string path)
        {
            if(path == "/")
            {
                foreach (string defaultPage in defaultPages)
                {
                    FileInfo info = new FileInfo(webRoot + "/" + defaultPage);
                    if(info.Exists)
                    {
                        path = "/" + defaultPage;
                        break;
                    }
                }
            }
            return webRoot + path;
        }

        public static void SendHeader(int iTotBytes, string sStatusCode, ref Socket mySocket)
        {
            String sBuffer = "";
            // if Mime type is not provided set default to text/html
           
            sBuffer = sBuffer + "HTTP/1.1 " + sStatusCode + "\r\n";
            sBuffer = sBuffer + "Server: My Little Server\r\n";
            sBuffer = sBuffer + "Accept-Ranges: bytes\r\n";
            sBuffer = sBuffer + "Content-Length: " + iTotBytes + "\r\n\r\n";
            Byte[] bSendData = Encoding.ASCII.GetBytes(sBuffer);
            mySocket.Send(Encoding.ASCII.GetBytes(sBuffer), Encoding.ASCII.GetBytes(sBuffer).Length, SocketFlags.None);
        }
    }
}
