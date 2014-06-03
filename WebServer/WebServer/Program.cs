using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using WebServer.Control;

namespace WebServer
{
    class Program
    {
        static void Main(string[] args)
        {
            new Web.WebServer(8080);
            Console.WriteLine("Webserver (127.0.0.1:8080) listening...");
            new ControlServer(8081);
            Console.WriteLine("Controlserver (127.0.0.1:8081) listening...");

            Console.Read();
        }
    }
}
