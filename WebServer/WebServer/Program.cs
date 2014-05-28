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
            new ControlServer(8081);
            Console.Read();
        }
    }
}
