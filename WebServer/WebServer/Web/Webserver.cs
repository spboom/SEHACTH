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
                    socket.SendFile(@"Web\index.html", null, null, TransmitFileOptions.Disconnect);
                    socket.Close();
                }
            }
        }
    }
}
