using Server.Control.SessionControl;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Server.Control
{
    class ControlServer : Server
    {
        public static readonly string CONTROLROOT = @"./Control";
        public static readonly string ADMINFORM = @"/settings";
        public static readonly string LOGIN = @"/login";
        public static readonly string LOG = @"/log";
        public static readonly string USERMANAGER = @"/usermanager";
        public static string[] CONTROLDEFAULTPAGES = new string[] { LOGIN };
        public static readonly bool CONTROLDIRECTORYBROWSING = false;
        private static readonly Semaphore settingsFile = new Semaphore(1, 1);


        public ControlServer(int port)
            : base(port, CONTROLROOT, CONTROLDEFAULTPAGES, CONTROLDIRECTORYBROWSING)
        {
        }

        protected override void run()
        {
            while (running)
            {
                try
                {
                    Socket socket = Listener.AcceptSocket();
                    if (socket.Connected)
                    {
                        WebRequests.WaitOne();
                        new Thread(() =>
                        {
                            ControlServerRequest request = new ControlServerRequest(socket, this);
                            openSockets.Add(request);
                            request.start();
                        }).Start();
                    }
                }
                catch { }
            }
        }

        public void saveSettings(int webServerPort, int controlServerPort, string webServerRoot, string[] webServerDefaultPages, bool webServerDirectoryBrowsing)
        {
            Program.updateSettings(webServerPort, controlServerPort, webServerRoot, webServerDefaultPages, webServerDirectoryBrowsing);
            settingsFile.WaitOne();
            string settings = "";
            settings += "<?xml version=\"1.0\" encoding=\"utf-8\" ?>\n";
            settings += "<Settings>\n";
            settings += "  <WebServer port=\"" + webServerPort + "\" />\n";
            settings += "  <ControlServer port=\"" + controlServerPort + "\" />\n";
            settings += "  <WebRoot path=\"" + webServerRoot + "\" />\n";
            settings += "  <DefaultPage>\n";
            foreach (string defaultPage in webServerDefaultPages)
            {
                settings += "    <File name=\"" + defaultPage + "\" />\n";
            }
            settings += "  </DefaultPage>\n";
            settings += "  <DirectoryBrowsing value=\"" + webServerDirectoryBrowsing + "\" />\n";
            settings += "</Settings>";
            using (StreamWriter sw = new StreamWriter(Program.settingsFilePath, false))
            {
                sw.Write(settings);
                sw.Flush();
            }
            settingsFile.Release();
        }

        public string getAdminForm()
        {
            string browseDirectory = "";
            if (Program.webServerDirectoryBrowsing)
            {
                browseDirectory = "checked";
            }
            string adminForm = "";
            adminForm += "<html>\n";
            adminForm += "  <head><title>control server</title></head>\n";
            adminForm += "  <body>\n";
            adminForm += "    <form method=\"POST\">\n";
            adminForm += "      <table>\n";
            adminForm += "        <thead><tr><th>SuperServer</th><th class=\"right\">Control Panel</th></tr></thead>\n";
            adminForm += "        <tbody>\n";
            adminForm += "          <tr><td>Web port:</td><td><input type=\"text\" name=\"webPort\" value=" + Program.WebServerPort + " /></td></tr>\n";
            adminForm += "          <tr><td>Control port:</td><td><input type=\"text\" name=\"controlPort\" value=" + Program.ControlServerPort + " /></td></tr>\n";
            adminForm += "          <tr><td>Webroot:</td><td><input type=\"text\" name=\"webRoot\" value=" + Program.WebServerRoot + "></td></tr>\n";
            adminForm += "          <tr><td>Default page:</td><td><input type=\"text\" name=\"defaultPage\" value=" + Program.WebServerDefaultPages + "></td></tr>\n";
            adminForm += "          <tr><td>Directory browsing</td><td><input type=\"checkbox\" name=\"dirBrowsing\" " + browseDirectory + "></td></tr>\n";
            adminForm += "          <tr><td><input type=\"submit\" name=\"submit\" value=\"OK\"></td><td class=\"right\"><input type=\"submit\" name=\"log\" value=\"Show Log\"></td></tr>\n";
            adminForm += "        </tbody>\n";
            adminForm += "      </table>\n";
            adminForm += "    </form>\n";
            adminForm += "  </body>\n";
            adminForm += "</html>";
            return adminForm;
        }

        public string getLoginForm()
        {
            string loginForm = "";

            loginForm += "<html>";
            loginForm += "  <head><title>Login</title></head>";
            loginForm += "  <body>";
            loginForm += "      <div>";
            loginForm += "          <h1>Login</h1>";
            loginForm += "          <form  method=\"post\">";
            loginForm += "              <input type=\"text\" name=\"username\" placeholder=\"Username\"><br>";
            loginForm += "              <input type=\"password\" name=\"password\" placeholder=\"Password\"><br>";
            loginForm += "              <input type=\"submit\" name=\"login\" value=\"login\"><br>";
            loginForm += "          </form>";
            loginForm += "          <a href=\"" + USERMANAGER + "\">Add users</a>";
            loginForm += "      </div>";
            loginForm += "  </body>";
            loginForm += "</html>";

            return loginForm;
        }

        public string getRegisterForm()
        {
            List<String> users = Authentication.allUsers();

            string registerForm = "";

            registerForm += "<html>";
            registerForm += "  <head><title>User Manager</title></head>";
            registerForm += "  <body>";
            registerForm += "      <div>";
            registerForm += "          <h1>Add users</h1>";
            registerForm += "          <form  method=\"post\">";
            registerForm += "              <input type=\"text\" name=\"username\" placeholder=\"Username\">";
            registerForm += "              <input type=\"password\" name=\"password\" placeholder=\"Password\">";
            registerForm += "              <input type=\"password\" name=\"confirm_password\" placeholder=\"Confirm Password\">";
            registerForm += "              <input type=\"submit\" name=\"add\" value=\"Add\">";
            registerForm += "          </form>";

            registerForm += "          <div>";
            registerForm += "            <form  method=\"post\">";
            foreach (String user in users)
            {
                registerForm += "           <input type=\"checkbox\" name=\"" + user + "\">" + user + "<br />";
            }
            registerForm += "              <br /><input type=\"submit\" name=\"remove\" value=\"Verwijder\">";
            registerForm += "            </form>";
            registerForm += "          </div>";

            registerForm += "      </div>";
            registerForm += "  </body>";
            registerForm += "</html>";

            return registerForm;
        }
    }
}