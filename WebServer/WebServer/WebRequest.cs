﻿using System;
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

        private LogItem LogItem { get; set; }


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

        protected void POST(string[] sBufferArray)
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
            byte[] messageByteArray = Encoding.ASCII.GetBytes(html);
            SendHeader(messageByteArray.Length, message);
            Socket.Send(messageByteArray);
        }

        public abstract string getFile(string path);

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