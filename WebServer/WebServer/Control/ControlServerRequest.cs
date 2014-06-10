using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace Server.Control
{
    class ControlServerRequest : WebRequest<ControlServer>
    {
        public ControlServerRequest(Socket socket, ControlServer server)
            : base(socket, server)
        { }

        protected override void POST(string[] sBufferArray)
        {
            try
            {
                int webPort = -1, controlPort = -1;
                string webRoot = null, defaultPage = null;
                bool dirBrowsing = false;
                string[] valueString = sBufferArray[sBufferArray.Length - 1].Split('&');
                string[][] values = new string[valueString.Length][];
                for (int i = 0; i < valueString.Length; i++)
                {
                    values[i] = valueString[i].Split('=');
                    switch (values[i][0])
                    {
                        case "webPort":
                            webPort = int.Parse(values[i][1]);
                            break;
                        case "controlPort":
                            controlPort = int.Parse(values[i][1]);
                            break;
                        case "webRoot":
                            webRoot = values[i][1];
                            break;
                        case "defaultPage":
                            defaultPage = values[i][1];
                            break;
                        case "dirBrowsing":
                            dirBrowsing = values[i][1] == "on";
                            break;
                        default:
                            break;
                    }
                }
                webRoot = Uri.UnescapeDataString(webRoot);
                defaultPage = Uri.UnescapeDataString(defaultPage);
                string[] defaultPageArray = defaultPage.Split(';');

                ServerInstance.saveSettings(webPort, controlPort, webRoot, defaultPageArray, dirBrowsing);
            }
            catch (Exception)
            {
                sendError(400, "Bad Data");
            }
            SendHeader(0, 200, "POST OK");
        }

        protected override void GET(string[] sBufferArray)
        {
            base.GET(sBufferArray);
            //sendString(Logger.Logger.Instance.readFile(), "200 OK LOG");
        }

        public override void sendFile(string path)
        {
            if (path == "/")
            {
                sendString(ServerInstance.getAdminForm(), 200, "OK");
            }
            else
            {
                base.sendFile(path);
            }
        }

        public override void close()
        {
            base.close();
            ServerInstance.EndRequest(this);
        }
    }
}
