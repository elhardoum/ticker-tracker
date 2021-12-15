using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace TickerTracker.Models
{
    public class Tweets
    {
        public static readonly string[] Columns = new string[]{
            "Id",
            "Text",
            "Url",
            "PortfolioId",
            "Created",
        };

        public static async Task<List<Tweet>> findByField(string field, string value)
        {
            var items = new List<Tweet>();

            if (-1 == Array.IndexOf(Columns, field))
                throw new Exception("Unknown Portfolio column passed");

            await Database.Query(async (conn) =>
            {
                String query = $@"select t.*, p.Symbol, p.IsCrypto, p.[Percent], p.UserId from Tweets t
                    join Portfolio p on p.Id = t.PortfolioId
                    where t.{field} = @val";

                SqlCommand command = new SqlCommand(query, conn);

                command.Parameters.Add(new SqlParameter("@val", value));

                try
                {
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (reader.Read())
                        {
                            var item = parse(reader);

                            if ( item.Id > 0 )
                                items.Add(item);
                        }
                    }
                }
                catch (SqlException e)
                {
                    Console.WriteLine("Tweets.findOne error: {0}, {1}", e.ToString(), e.Message);
                }
            });

            return items;
        }

        public static async Task<List<Tweet>> findByUserId(long UserId)
        {
            var items = new List<Tweet>();

            if ( UserId > 0 )
            {
                await Database.Query(async (conn) =>
                {
                    String query = @"select t.*, p.Symbol, p.IsCrypto, p.[Percent], p.UserId from Tweets t
                        join Portfolio p on p.Id = t.PortfolioId
                        where t.Id in (select Id from Portfolio where UserId = @UserId)";

                    SqlCommand command = new SqlCommand(query, conn);

                    command.Parameters.Add(new SqlParameter("@UserId", UserId));

                    try
                    {
                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (reader.Read())
                            {
                                var item = parse(reader);

                                if ( item.Id > 0 )
                                    items.Add(item);
                            }
                        }
                    }
                    catch (SqlException e)
                    {
                        Console.WriteLine("Tweets.findOne error: {0}, {1}", e.ToString(), e.Message);
                    }
                });
            }

            return items;
        }

        public static Tweet parse(SqlDataReader reader) => new Tweet
        {
            Id = long.Parse(reader["Id"].ToString()), // long
            Text = reader["Text"].ToString(), // string
            Url = reader["Url"].ToString(), // string
            PortfolioId = long.Parse(reader["PortfolioId"].ToString()), // long
            Created = DateTime.Parse(reader["Created"].ToString()), // DateTime
            PortfolioItemMin = new PortfolioItem
            {
                UserId = long.Parse(reader["UserId"].ToString()),
                Symbol = reader["Symbol"].ToString(),
                IsCrypto = int.Parse(reader["IsCrypto"].ToString()),
                Percent = double.Parse(reader["Percent"].ToString()),
            },
        };
    }
}
