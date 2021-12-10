using System;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace TickerTracker.Models
{
    public class Option
    {
        public string Name;
        public string Value;
        public DateTime Updated;

        public async Task<bool> Save()
        {
            bool done = false;

            await Database.Query(async (conn) =>
            {
                String query = @"if exists ( select 1 from Options where Name=@Name )
                    update Options set Value=@Value, Updated=GETDATE() where Name=@Name;
                else
                    insert into Options (Name, Value, Updated)
                        values (@Name, @Value, GETDATE());";

                SqlCommand command = new SqlCommand(query, conn);

                command.Parameters.Add(new SqlParameter("@Name", Name));
                command.Parameters.Add(new SqlParameter("@Value", Value));

                try
                {
                    done = await command.ExecuteNonQueryAsync() > 0;
                }
                catch (SqlException e)
                {
                    Console.WriteLine("Option.Save error: {0}, {1}", e.ToString(), e.Message);
                }
            });

            return done;
        }

        public async Task<bool> Load()
        {
            bool loaded = false;

            await Database.Query(async (conn) =>
            {
                String query = $"select top 1 * from Options where Name = @Name";

                SqlCommand command = new SqlCommand(query, conn);

                command.Parameters.Add(new SqlParameter("@Name", Name));

                try
                {
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        if (reader.Read())
                        {
                            Name = reader["Name"].ToString();
                            Value = reader["Value"].ToString();
                            Updated = DateTime.Parse(reader["Updated"].ToString());
                            loaded = ! string.IsNullOrEmpty(reader["Name"].ToString());
                        }
                    }
                }
                catch (SqlException e)
                {
                    Console.WriteLine("Option.Load error: {0}, {1}", e.ToString(), e.Message);
                }
            });

            return loaded;
        }

        public async Task<bool> Delete()
        {
            bool done = false;

            await Database.Query(async (conn) =>
            {
                String query = @"delete from Options where Name=@Name";

                SqlCommand command = new SqlCommand(query, conn);

                command.Parameters.Add(new SqlParameter("@Name", Name));

                try
                {
                    done = await command.ExecuteNonQueryAsync() > 0;
                }
                catch (SqlException e)
                {
                    Console.WriteLine("Option.Delete error: {0}, {1}", e.ToString(), e.Message);
                }
            });

            return done;
        }
    }
}
