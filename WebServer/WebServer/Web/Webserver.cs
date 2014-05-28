using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace WebServer.Web
{
    class Webserver
    {
        TcpListener server;

        public Webserver(Int32 port)
        {
            IPAddress ip = IPAddress.Parse("127.0.0.1");

            server = new TcpListener(ip, port);
        }
    }
}
