using System;
using System.Data;
using System.Data.SqlClient;

namespace TickerTracker.Models
{
    public class User
    {
        public long Id;
        public string Handle;
        public string Name;
        public string Avatar;
        public string Token;
        public string Secret;

        public bool Save()
        {
            bool saved = false;

            Database.Query(conn =>
            {
                String query = @"if exists ( select 1 from Users where Id=@Id )
                    update Users set Handle=@Handle, Name=@Name, Avatar=@Avatar, Token=@Token, Secret=@Secret
                        where Id=@Id;
                else
                    insert into Users (Id, Handle, Name, Avatar, Token, Secret)
                        values (@Id, @Handle, @Name, @Avatar, @Token, @Secret);";

                SqlCommand command = new SqlCommand(query, conn);

                command.Parameters.Add("@Id", (SqlDbType) Enum.Parse(typeof(SqlDbType), Id.ToString(), true));
                command.Parameters.Add("@Handle", (SqlDbType) Enum.Parse(typeof(SqlDbType), Handle, true));
                command.Parameters.Add("@Name", (SqlDbType) Enum.Parse(typeof(SqlDbType), Name, true));
                command.Parameters.Add("@Avatar", (SqlDbType) Enum.Parse(typeof(SqlDbType), Avatar, true));
                command.Parameters.Add("@Token", (SqlDbType) Enum.Parse(typeof(SqlDbType), Token, true));
                command.Parameters.Add("@Secret", (SqlDbType) Enum.Parse(typeof(SqlDbType), Secret, true));

                try
                {
                    saved = command.ExecuteNonQuery() > 0;
                }
                catch (SqlException e)
                {
                    Console.WriteLine("User.Save error: {0}, {1}", e.ToString(), e.Message);
                }
            });

            return saved;
        }
    }
}
