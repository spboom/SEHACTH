using Server.Control.SessionControl;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
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
            try
            {
                string path = sBufferArray[0].Split(' ')[1];

                if (path == ControlServer.ADMINFORM) { postAdminForm(sBufferArray); }
                else if (path == ControlServer.LOGIN) { postLoginForm(sBufferArray); }
                else if (path == ControlServer.USERMANAGER) { postRegisterForm(sBufferArray); }
            }
            catch (Exception)
            {
                sendError(400, "Bad Data");
            }
            SendHeader(0, 200, "POST OK", null);
        }

        private void postAdminForm(string[] sBufferArray)
        {
            int webPort = -1, controlPort = -1;
            string webRoot = null, defaultPage = null;
            bool dirBrowsing = false, post = true;
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
                    case "submit":
                        post = true;
                        break;
                    case "log":
                        post = false;
                        break;
                    default:
                        break;
                }
            }

            if (post)
            {
                webRoot = Uri.UnescapeDataString(webRoot);
                defaultPage = Uri.UnescapeDataString(defaultPage);
                string[] defaultPageArray = defaultPage.Split(';');

                ((ControlServer)ServerInstance).saveSettings(webPort, controlPort, webRoot, defaultPageArray, dirBrowsing);
            }
            else
            {
                sendRedirect(ControlServer.LOG);
            }
        }

        private void postLoginForm(string[] sBufferArray)
        {
            string[] formData = sBufferArray[12].Split('&');
            string username = formData[0].Split('=')[1];
            string password = formData[1].Split('=')[1];

            if (Authentication.verifyUser(username, password))
            {
                sendRedirect(ControlServer.ADMINFORM);
            }
            else
            {
                sendRedirect(ControlServer.LOGIN);
            }
        }

        private void postRegisterForm(string[] sBufferArray)
        {

            //"James=on&Sjors=on&remove=Verwijder"
            // split on &
            // foreach split on =
            // if [split.length][1] == Verwijder
            // if [1] == on

            string[] data = sBufferArray[sBufferArray.Length - 1].Split('&');
            string[][] values = new string[data.Length][];
            bool add = false, remove = false;
            for (int i = 0; i < data.Length; i++)
            {
                values[i] = data[i].Split('=');
                if (values[i][0] == "remove")
                {
                    remove = true;
                }
                else if (values[i][0] == "add")
                {
                    add = true;
                }
            }
            if (add && remove)
            {
                sendError(400, "Bad Data");
            }
            else if (add)
            {
                string[] formData = sBufferArray[12].Split('&');
                string username = formData[0].Split('=')[1];
                string password = formData[1].Split('=')[1];
                string confirmPassword = formData[2].Split('=')[1];

                if (password == confirmPassword && password != "" && confirmPassword != "" && username != "")
                {
                    // if not true: bad data? (exception)
                    if (Authentication.newUser(username, password))
                    {
                        Console.WriteLine("User " + username + " created");
                    }
                    else { sendError(500, "Failed to create user"); }

                    sendRedirect(ControlServer.USERMANAGER);
                }
                else
                {
                    sendError(400, "Bad Data");
                }
            }
            else if (remove)
            {
                int max = values.GetUpperBound(0);
                for (int i = 0; i < max; i++)
                {
                    if (values[i][0] != "admin" && values[i][1] == "on")
                    {
                        Authentication.removeUser(values[i][0]);
                    }


                }
                sendRedirect(ControlServer.USERMANAGER);
            }
        }

        protected override void GET(string[] sBufferArray)
        {
            base.GET(sBufferArray);
        }

        public override void sendFile(string path)
        {
            if (path == "/") { sendRedirect(ControlServer.LOGIN); }
            else if (path == ControlServer.LOGIN) { sendHTMLString(((ControlServer)ServerInstance).getLoginForm(), 200, "OK"); }
            else if (path == ControlServer.USERMANAGER) { sendHTMLString(((ControlServer)ServerInstance).getRegisterForm(), 200, "OK"); }
            else if (path == ControlServer.LOG) { sendString(Logger.Logger.Instance.readFile(), 200, "OK LOG"); }
            else if (path == ControlServer.ADMINFORM) { sendHTMLString(((ControlServer)ServerInstance).getAdminForm(), 200, "OK"); }
            else { base.sendFile(path); }
        }

        public override void close()
        {
            ServerInstance.EndRequest(this);
            base.close();
        }

        protected override string getFile(string path)
        {
            if (path == "/")
            {
                return ServerInstance.DefaultPages[0];
            }
            return base.getFile(path);
        }

    }
}
