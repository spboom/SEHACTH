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
using Server.DataBaseDataSetTableAdapters;
using System.Data.SqlClient;
using System.Security.Cryptography;

namespace Server
{
    class Program
    {

        private static WebServer webServer;
        private static ControlServer controlServer;
        public static readonly string settingsFilePath = @"Control/Settings.xml";
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
            settingsDoc.Load(settingsFilePath);

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

            //dbTest();

            Console.Read();
        }

        //private static void dbTest()
        //{
        //    /////////////////////

        //    // Generate username for test. Usernames must be unique.
        //    var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        //    var random = new Random();
        //    var user = new string(
        //        Enumerable.Repeat(chars, 8)
        //                  .Select(s => s[random.Next(s.Length)])
        //                  .ToArray());

        //    // Add new user
        //    Console.WriteLine("\nUser added: " + user);
        //    newUser(user, "p@$$w0rd");

        //    // Check if user exists
        //    Console.WriteLine("User exists: " + verifyUser(user, "p@$$w0rd"));
        //    Console.WriteLine("User doesn't exist: " + verifyUser(user, "abc"));

        //    /////////////////////
        //}



        public static int WebServerPort { get { return webServer.Port; } }

        public static int ControlServerPort { get { return controlServer.Port; } }

        public static string WebServerRoot { get { return webServer.WebRoot; } }

        //TODO dicide if needs to be moved
        public static string WebServerDefaultPages
        {
            get
            {
                string defaultPages = "";
                foreach (string defaultPage in webServer.DefaultPages)
                {
                    if (defaultPages != "")
                    {
                        defaultPages += ";";
                    }
                    defaultPages += defaultPage;
                }
                return defaultPages;
            }
        }

        public static void updateSettings(int webServerPort, int controlServerPort, string webServerRoot, string[] webServerDefaultPages, bool webServerDirectoryBrowsing)
        {
            webServer.close();
            controlServer.close();
            webServer = new WebServer(webServerPort, webServerRoot, webServerDefaultPages, webServerDirectoryBrowsing);
            controlServer = new ControlServer(controlServerPort);
        }


        public static bool webServerDirectoryBrowsing { get; set; }
    }
}
