using System;
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
        static readonly string CONTROLROOT = @"./Control";
        static readonly string[] CONTROLDEFAULTPAGES = new string[] { "adminForm.html" };
        static readonly bool CONTROLDIRECTORYBROWSING = false;
        static readonly Semaphore settingsFile = new Semaphore(1, 1);

        public ControlServer(int port)
            : base(port, CONTROLROOT, CONTROLDEFAULTPAGES, CONTROLDIRECTORYBROWSING)
        { }

        protected override void run()
        {
            while (true)
            {
                Socket socket = Listener.AcceptSocket();
                if (socket.Connected)
                {
                    WebRequests.WaitOne();
                    new Thread(() => new ControlServerRequest(socket, this)).Start();
                }
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
            adminForm += "          <tr><td><input type=\"button\" name=\"log\" value=\"Show Log\"></td><td class=\"right\"><input type=\"submit\" name=\"submit\" value=\"OK\"></td></tr>\n";
            adminForm += "        </tbody>\n";
            adminForm += "      </table>\n";
            adminForm += "    </form>\n";
            adminForm += "  </body>\n";
            adminForm += "</html>";
            return adminForm;
        }
    }
}
