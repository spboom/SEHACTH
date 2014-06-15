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

            if (verifyUser(username, password))
            {
                // change path (/login -> /) (not working)
                // create session
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
                newUser(username, password);
                Console.WriteLine("User " + username + " created: " + verifyUser(username, password));

                // change path (/register -> /login) (not working)
                sendString(ServerInstance.getLoginForm(), 200, "OK");
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

            // change
            comm.Parameters.AddWithValue("@firstname", "Bob");
            comm.Parameters.AddWithValue("@middlename", "");
            comm.Parameters.AddWithValue("@lastname", "Smith");

            // Support
            comm.Parameters.AddWithValue("@roleId", 2);

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
    }
}
