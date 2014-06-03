using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WebServer.Web
{
    class WebServer
    {
        TcpListener server;

        public WebServer(Int32 port)
        {
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

                    if (sBuffer.Substring(0, 4) == "HEAD")
                    {
                        Console.WriteLine("HEAD request");
                        // return 400
                    }
                    else if (sBuffer.Substring(0, 3) == "PUT")
                    {
                        Console.WriteLine("PUT request");
                        // return 400
                    }

                    socket.SendFile(@"Web\index.html", null, null, TransmitFileOptions.Disconnect);
                    socket.Close();

                }
            }
        }

    }
}
