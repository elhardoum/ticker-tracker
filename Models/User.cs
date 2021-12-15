using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;

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
        public string SessionId;

        public async Task<bool> Save()
        {
            bool saved = false;

            await Database.Query(async (conn) =>
            {
                String query = @"if exists ( select 1 from Users where Id=@Id )
                    update Users set Handle=@Handle, Name=@Name, Avatar=@Avatar, Token=@Token, Secret=@Secret, [SessionId]=@SessionId
                        where Id=@Id;
                else
                    insert into Users (Id, Handle, Name, Avatar, Token, Secret, [SessionId])
                        values (@Id, @Handle, @Name, @Avatar, @Token, @Secret, @SessionId);";

                SqlCommand command = new SqlCommand(query, conn);

                command.Parameters.Add(new SqlParameter("@Id", Id));
                command.Parameters.Add(new SqlParameter("@Handle", Handle));
                command.Parameters.Add(new SqlParameter("@Name", Name));
                command.Parameters.Add(new SqlParameter("@Avatar", Avatar));
                command.Parameters.Add(new SqlParameter("@Token", Token));
                command.Parameters.Add(new SqlParameter("@Secret", Secret));
                command.Parameters.Add(new SqlParameter("@SessionId", SessionId));

                try
                {
                    saved = await command.ExecuteNonQueryAsync() > 0;
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
