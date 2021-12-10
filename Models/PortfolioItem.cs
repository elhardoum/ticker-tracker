using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace TickerTracker.Models
{
    public class PortfolioItem
    {
        public long Id;
        public long UserId;
        public string Symbol;
        public int IsCrypto;
        public int Enabled;
        public double Percent;
        public string TweetText;
        public DateTime Updated;

        public async Task<bool> Save()
        {
            long savedId = 0;

            await Database.Query(async (conn) =>
            {
                string query = null;

                if ( Id > 0 ) {
                    query = @"if exists ( select top 1 * from Portfolio where Id=@Id )
                        update Portfolio set
                            UserId=@UserId,
                            Symbol=@Symbol,
                            IsCrypto=@IsCrypto,
                            Enabled=@Enabled,
                            [Percent]=@Percent,
                            TweetText=@TweetText,
                            Updated=@Updated
                        output Inserted.Id
                        where Id=@Id;
                    else
                        insert into Portfolio (UserId, Symbol, IsCrypto, Enabled, [Percent], TweetText, Updated)
                            output Inserted.Id
                            values (@UserId, @Symbol, @IsCrypto, @Enabled, @Percent, @TweetText, @Updated);";
                } else {
                    query = @"insert into Portfolio (UserId, Symbol, IsCrypto, Enabled, [Percent], TweetText, Updated)
                            output Inserted.Id
                            values (@UserId, @Symbol, @IsCrypto, @Enabled, @Percent, @TweetText, @Updated);";
                }

                SqlCommand command = new SqlCommand(query, conn);

                command.Parameters.Add(new SqlParameter("@Id", Id));
                command.Parameters.Add(new SqlParameter("@UserId", UserId));
                command.Parameters.Add(new SqlParameter("@Symbol", Symbol));
                command.Parameters.Add(new SqlParameter("@IsCrypto", IsCrypto));
                command.Parameters.Add(new SqlParameter("@Enabled", Enabled));
                command.Parameters.Add(new SqlParameter("@Percent", Percent));
                command.Parameters.Add(new SqlParameter("@TweetText", null == TweetText ? "" : TweetText));
                command.Parameters.Add(new SqlParameter("@Updated", Updated == DateTime.MinValue ? DateTime.Now : Updated));

                try
                {
                    savedId = (long) (await command.ExecuteScalarAsync());
                }
                catch (SqlException e)
                {
                    Console.WriteLine("PortfolioItem.Save error: {0}, {1}", e.ToString(), e.Message);
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
                String query = $"select top 1 * from Portfolio where Id = @Id";

                SqlCommand command = new SqlCommand(query, conn);

                command.Parameters.Add(new SqlParameter("@Id", Id));

                try
                {
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        if (reader.Read())
                        {
                            Id = long.Parse(reader["Id"].ToString()); // long
                            UserId = long.Parse(reader["UserId"].ToString()); // long
                            Symbol = reader["Symbol"].ToString(); // string
                            IsCrypto = int.Parse(reader["IsCrypto"].ToString()); // int
                            Enabled = int.Parse(reader["Enabled"].ToString()); // int
                            Percent = double.Parse(reader["Percent"].ToString()); // double
                            TweetText = reader["TweetText"].ToString(); // string
                            Updated = DateTime.Parse(reader["Updated"].ToString()); // DateTime
                            loaded = Id > 0;
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

            if ( Id > 0 )
            {
                await Database.Query(async (conn) =>
                {
                    String query = @"delete from Portfolio where Id=@Id";

                    SqlCommand command = new SqlCommand(query, conn);

                    command.Parameters.Add(new SqlParameter("@Id", Id));

                    try
                    {
                        done = await command.ExecuteNonQueryAsync() > 0;
                    }
                    catch (SqlException e)
                    {
                        Console.WriteLine("PortfolioItem.Delete error: {0}, {1}", e.ToString(), e.Message);
                    }
                });
            }

            return done;
        }
    }
}
