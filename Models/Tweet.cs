using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace TickerTracker.Models
{
    public class Tweet
    {
        public long Id;
        public string Text;
        public string Url;
        public long PortfolioId;
        public DateTime Created;
        public PortfolioItem PortfolioItemMin;

        public async Task<bool> Save()
        {
            long savedId = 0;

            await Database.Query(async (conn) =>
            {
                string query = null;

                if ( Id > 0 ) {
                    query = @"if exists ( select top 1 * from Tweets where Id=@Id )
                        update Tweets set
                            [Text]=@Text,
                            Url=@Url,
                            PortfolioId=@PortfolioId,
                            Created=@Created
                        output Inserted.Id
                        where Id=@Id;
                    else
                        insert into Tweets ([Text], Url, PortfolioId, Created)
                            output Inserted.Id
                            values (@Text, @Url, @PortfolioId, @Created);";
                } else {
                    query = @"insert into Tweets ([Text], Url, PortfolioId, Created)
                            output Inserted.Id
                            values (@Text, @Url, @PortfolioId, @Created);";
                }

                SqlCommand command = new SqlCommand(query, conn);

                command.Parameters.Add(new SqlParameter("@Id", Id));
                command.Parameters.Add(new SqlParameter("@Text", null == Text ? "" : Text));
                command.Parameters.Add(new SqlParameter("@Url", null == Url ? "" : Url));
                command.Parameters.Add(new SqlParameter("@PortfolioId", PortfolioId));
                command.Parameters.Add(new SqlParameter("@Created", Created == DateTime.MinValue ? DateTime.Now : Created));

                try
                {
                    savedId = (long) (await command.ExecuteScalarAsync());
                }
                catch (SqlException e)
                {
                    Console.WriteLine("Tweet.Save error: {0}, {1}", e.ToString(), e.Message);
                }
            });

            if ( savedId > 0)
            {
                Id = savedId;
            }

            return savedId > 0;
        }

        public async Task<bool> Load()
        {
            bool loaded = false;

            if ( ! ( Id > 0 ) )
                throw new Exception("Id cannot be null");

            await Database.Query(async (conn) =>
            {
                String query = $"select top 1 * from Tweets where Id = @Id";

                SqlCommand command = new SqlCommand(query, conn);

                command.Parameters.Add(new SqlParameter("@Id", Id));

                try
                {
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        if (reader.Read())
                        {
                            Id = long.Parse(reader["Id"].ToString()); // long
                            Text = reader["Text"].ToString(); // string
                            Url = reader["Url"].ToString(); // string
                            PortfolioId = long.Parse(reader["PortfolioId"].ToString()); // long
                            Created = DateTime.Parse(reader["Created"].ToString()); // DateTime
                            loaded = Id > 0;
                        }
                    }
                }
                catch (SqlException e)
                {
                    Console.WriteLine("Tweet.Load error: {0}, {1}", e.ToString(), e.Message);
                }
            });

            return loaded;
        }

        public async Task<bool> Delete()
        {
            bool done = false;

            if ( Id > 0 )
            {
                await Database.Query(async (conn) =>
                {
                    String query = @"delete from Tweets where Id=@Id";

                    SqlCommand command = new SqlCommand(query, conn);

                    command.Parameters.Add(new SqlParameter("@Id", Id));

                    try
                    {
                        done = await command.ExecuteNonQueryAsync() > 0;
                    }
                    catch (SqlException e)
                    {
                        Console.WriteLine("Tweet.Delete error: {0}, {1}", e.ToString(), e.Message);
                    }
                });
            }

            return done;
        }
    }
}
