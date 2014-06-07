using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace Server.Control
{
    class ControlServerRequest : WebRequest
    {
        public ControlServerRequest(Socket socket, ControlServer server)
            : base(socket, server)
        { }

        protected override void POST(string[] sBufferArray)
        {
            SendHeader(0, "200 POST OK");
        }

        protected override void GET(string[] sBufferArray)
        {
            sendString(Logger.Logger.Instance.readFile(), "200 OK LOG");
        }
    }
}
