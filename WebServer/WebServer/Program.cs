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

        private static void dbTest()
        {
            /////////////////////

            // Generate username for test. Usernames must be unique.
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            var user = new string(
                Enumerable.Repeat(chars, 8)
                          .Select(s => s[random.Next(s.Length)])
                          .ToArray());

            // Add new user
            Console.WriteLine("\nUser added: " + user);
            newUser(user, "p@$$w0rd");

            // Check if user exists
            Console.WriteLine("User exists: " + verifyUser(user, "p@$$w0rd"));
            Console.WriteLine("User doesn't exist: " + verifyUser(user, "abc"));

            /////////////////////
        }

        public static String sha256_hash(String value)
        {
            StringBuilder Sb = new StringBuilder();

            using (SHA256 hash = SHA256Managed.Create())
            {
                Encoding enc = Encoding.UTF8;
                Byte[] result = hash.ComputeHash(enc.GetBytes(value));

                foreach (Byte b in result)
                    Sb.Append(b.ToString("x2"));
            }

            return Sb.ToString();
        }

        // new user
        public static void newUser(String username, String password)
        {
            SqlConnection conn = new SqlConnection(@"Data Source=(LocalDB)\v11.0;AttachDbFilename=|DataDirectory|\DataBase.mdf;Integrated Security=True");
            String sql = "INSERT INTO [User] (UserName, Password, Salt, FirstName, MiddleName, LastName, Role_id) VALUES (@username, @password, @salt, @firstname, @middlename, @lastname, @roleId)";
            SqlCommand comm = new SqlCommand(sql, conn);

            // Generate salt value
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            var result = new string(
                Enumerable.Repeat(chars, 20)
                          .Select(s => s[random.Next(s.Length)])
                          .ToArray());
            var salt = sha256_hash(result);

            var hashed_password = sha256_hash(salt + password);

            comm.Parameters.AddWithValue("@username", username);
            comm.Parameters.AddWithValue("@password", hashed_password);
            comm.Parameters.AddWithValue("@salt", salt);

            comm.Parameters.AddWithValue("@firstname", "Bob");
            comm.Parameters.AddWithValue("@middlename", "");
            comm.Parameters.AddWithValue("@lastname", "Smith");

            comm.Parameters.AddWithValue("@roleId", 1);

            try
            {
                conn.Open();
                comm.ExecuteNonQuery();
                conn.Close();
            }
            catch (Exception ex)
            {
                throw (ex);
            }
            finally
            {
                conn.Close();
            }
        }

        // check user
        public static Boolean verifyUser(String username, String password)
        {
            SqlConnection connection = new SqlConnection(@"Data Source=(LocalDB)\v11.0;AttachDbFilename=|DataDirectory|\DataBase.mdf;Integrated Security=True");
            SqlCommand command = new SqlCommand();
            SqlDataReader reader;

            command.Connection = connection;
            command.CommandText = "SELECT Password, Salt FROM [User] WHERE UserName = @username";
            command.Parameters.AddWithValue("@username", username);

            connection.Open();
            reader = command.ExecuteReader();

            string dbPassword = "";
            string dbSalt = "";

            while (reader.Read())
            {
                dbPassword = reader["Password"].ToString();
                dbSalt = reader["Salt"].ToString();
            }

            reader.Close();
            connection.Close();

            return (dbPassword.Equals(sha256_hash(dbSalt + password)));

        }

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
