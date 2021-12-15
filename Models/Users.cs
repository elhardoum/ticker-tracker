using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace TickerTracker.Models
{
    public class Users
    {
        public static string[] Columns = new string[]{
            "Id",
            "Handle",
            "Name",
            "Avatar",
            "Token",
            "Secret",
            "SessionId",
        };

        public static async Task<User> findOne(string field, string value)
        {
            var user = new User();

            if (-1 == Array.IndexOf(Columns, field))
                throw new Exception("Unknown Users column passed");

            await Database.Query(async (conn) =>
            {
                String query = $"select top 1 * from Users where [{field}] = @val";

                SqlCommand command = new SqlCommand(query, conn);

                command.Parameters.Add(new SqlParameter("@val", value));

                try
                {
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        if (reader.Read())
                        {
                            user = new User
                            {
                                Id = long.Parse(reader["Id"].ToString()),
                                Handle = reader["Handle"].ToString(),
                                Name = reader["Name"].ToString(),
                                Avatar = reader["Avatar"].ToString(),
                                Token = reader["Token"].ToString(),
                                Secret = reader["Secret"].ToString(),
                                SessionId = reader["SessionId"].ToString(),
                            };
                        }
                    }
                }
                catch (SqlException e)
                {
                    Console.WriteLine("Users.findOne error: {0}, {1}", e.ToString(), e.Message);
                }
            });

            return user.Id > 0 ? user : null;
        }
    }
}
