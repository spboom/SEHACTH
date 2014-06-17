using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Server.Control
{
    class Authentication
    {
        public static Boolean newUser(String username, String password)
        {
            SqlConnection conn = new SqlConnection(@"Data Source=(LocalDB)\v11.0;AttachDbFilename=|DataDirectory|\DataBase.mdf;Integrated Security=True");
            String sql = "INSERT INTO [Users] (UserName, Password, Salt, Role_id) VALUES (@username, @password, @salt, @roleId)";
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
            comm.Parameters.AddWithValue("@roleId", 2); // Support

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

            return verifyUser(username, password) ? true : false;
        }

        public static Boolean verifyUser(String username, String password)
        {
            SqlConnection connection = new SqlConnection(@"Data Source=(LocalDB)\v11.0;AttachDbFilename=|DataDirectory|\DataBase.mdf;Integrated Security=True");
            SqlCommand command = new SqlCommand();
            SqlDataReader reader;

            command.Connection = connection;
            command.CommandText = "SELECT Password, Salt FROM [Users] WHERE UserName = @username";
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


        public static List<String> allUsers()
        {
            List<String> users = new List<String>();

            SqlConnection connection = new SqlConnection(@"Data Source=(LocalDB)\v11.0;AttachDbFilename=|DataDirectory|\DataBase.mdf;Integrated Security=True");
            SqlCommand command = new SqlCommand();
            SqlDataReader reader;

            command.Connection = connection;
            command.CommandText = "SELECT UserName FROM [Users]";
            connection.Open();
            reader = command.ExecuteReader();

            while (reader.Read())
            {
                users.Add(reader["UserName"].ToString());
            }

            reader.Close();
            connection.Close();

            return users;
        }

        public static void removeUser(string username)
        {
            SqlConnection connection = new SqlConnection(@"Data Source=(LocalDB)\v11.0;AttachDbFilename=|DataDirectory|\DataBase.mdf;Integrated Security=True");
            SqlCommand command = new SqlCommand();

            command.Connection = connection;
            command.CommandText = "DELETE FROM [Users] WHERE UserName = @username";
            command.Parameters.AddWithValue("@username", username);

            connection.Open();
            command.ExecuteNonQuery();
            connection.Close();
        }
    }
}
