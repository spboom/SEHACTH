using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
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
                string path = sBufferArray[0].Split(' ')[1];

                if (path == "/") { postAdminForm(sBufferArray); }
                else if (path == "/login") { postLoginForm(sBufferArray); }
                else if (path == "/register") { postRegisterForm(sBufferArray); }
            }
            catch (Exception)
            {
                sendError(400, "Bad Data");
            }
            SendHeader(0, 200, "POST OK");
        }

        private void postAdminForm(string[] sBufferArray)
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

        private void postLoginForm(string[] sBufferArray)
        {
            string[] formData = sBufferArray[12].Split('&');
            string username = formData[0].Split('=')[1];
            string password = formData[1].Split('=')[1];

            if (Authentication.verifyUser(username, password))
            {
                // change path (/login -> /) (not working)
                // create session
                // example: web.Session["username"] = web.Post("username");
                //socket.Session...
                sendString(ServerInstance.getAdminForm(), 200, "OK");
            }
            else
            {
                sendString(ServerInstance.getLoginForm(), 200, "OK");
            }
        }

        private void postRegisterForm(string[] sBufferArray)
        {
            string[] formData = sBufferArray[12].Split('&');
            string username = formData[0].Split('=')[1];
            string password = formData[1].Split('=')[1];
            string confirmPassword = formData[2].Split('=')[1];

            if (password == confirmPassword)
            {
                // if not true: bad data? (exception)
                if (Authentication.newUser(username, password))
                {
                    Console.WriteLine("User " + username + " created");
                }
            }
            else
            {
                sendString(ServerInstance.getAdminForm(), 200, "OK");
            }
        }

        protected override void GET(string[] sBufferArray)
        {
            base.GET(sBufferArray);
            //sendString(Logger.Logger.Instance.readFile(), "200 OK LOG");
        }

        public override void sendFile(string path)
        {
            if (path == "/") { sendString(ServerInstance.getAdminForm(), 200, "OK"); }
            else if (path == "/login") { sendString(ServerInstance.getLoginForm(), 200, "OK"); }
            else if (path == "/register") { sendString(ServerInstance.getRegisterForm(), 200, "OK"); }
            else { base.sendFile(path); }
        }

        public override void close()
        {
            base.close();
            ServerInstance.EndRequest(this);
        }

    }
}
