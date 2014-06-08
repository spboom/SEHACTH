using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Server.Control;
using Server.Web;
using Server.Logger;
using System.IO;

namespace Server
{
    class Program
    {

        private static WebServer webServer;
        private static ControlServer controlServer;
        static void Main(string[] args)
        {
            initServer();
        }

        private static void initServer()
        {
            int webPort = 0, controlPort = 0;
            string webRoot;
            List<String> defaultpages = new List<String>();
            bool directoryBrowsing;

            XmlDocument settingsDoc = new XmlDocument();
            settingsDoc.Load(@"Control/Settings.xml");

            XmlElement settingsElement = settingsDoc.DocumentElement;

            XmlNodeList settingsNodes = settingsElement.ChildNodes;

            XmlNode webServerNode = settingsElement.GetElementsByTagName("WebServer")[0];
            int.TryParse(webServerNode.Attributes[0].Value, out webPort);

            XmlNode controlServerNode = settingsElement.GetElementsByTagName("ControlServer")[0];
            int.TryParse(controlServerNode.Attributes[0].Value, out controlPort);

            XmlNode webbRootNode = settingsElement.GetElementsByTagName("WebRoot")[0];
            webRoot = webbRootNode.Attributes[0].Value;

            XmlNodeList defaultPagesNodeList = settingsElement.GetElementsByTagName("File");
            foreach (XmlNode fileNode in defaultPagesNodeList)
            {
                defaultpages.Add(fileNode.Attributes[0].Value);
            }

            XmlNode directoryBrowsingNode = settingsElement.GetElementsByTagName("DirectoryBrowsing")[0];
            bool.TryParse(directoryBrowsingNode.Attributes[0].Value, out directoryBrowsing);

            webServer = new WebServer(webPort, webRoot, defaultpages.ToArray(), directoryBrowsing);
            Console.WriteLine("Webserver (127.0.0.1:" + webPort + ") listening...");
            controlServer = new ControlServer(controlPort);
            Console.WriteLine("Controlserver (127.0.0.1: " + controlPort + ") listening...");

            Logger.Logger logger = Logger.Logger.Instance;
            Console.WriteLine("Logger started...");

            Console.Read();
        }
    }
}
