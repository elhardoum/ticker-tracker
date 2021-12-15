using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace TickerTracker.Models
{
    public class PortfolioItems
    {
        public static readonly string[] Columns = new string[]{
            "Id",
            "UserId",
            "Symbol",
            "IsCrypto",
            "Enabled",
            "Percent",
            "TweetText",
            "Updated",
            "LastTweetedQuoteTimestamp",
        };

        public static async Task<List<PortfolioItem>> findByField(string field, string value)
        {
            var items = new List<PortfolioItem>();

            if (-1 == Array.IndexOf(Columns, field))
                throw new Exception("Unknown Portfolio column passed");

            await Database.Query(async (conn) =>
            {
                String query = $"select * from Portfolio where {field} = @val";

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
                    Console.WriteLine("PortfolioItems.findOne error: {0}, {1}", e.ToString(), e.Message);
                }
            });

            return items;
        }

        public static PortfolioItem parse(SqlDataReader reader) => new PortfolioItem
        {
            Id = long.Parse(reader["Id"].ToString()), // long
            UserId = long.Parse(reader["UserId"].ToString()), // long
            Symbol = reader["Symbol"].ToString(), // string
            IsCrypto = int.Parse(reader["IsCrypto"].ToString()), // int
            Enabled = int.Parse(reader["Enabled"].ToString()), // int
            Percent = double.Parse(reader["Percent"].ToString()), // double
            TweetText = reader["TweetText"].ToString(), // string
            Updated = DateTime.Parse(reader["Updated"].ToString()), // DateTime
            LastTweetedQuoteTimestamp =
                ! string.IsNullOrEmpty(reader["LastTweetedQuoteTimestamp"].ToString())
                ? DateTime.Parse(reader["LastTweetedQuoteTimestamp"].ToString())
                : DateTime.MinValue, // DateTime
        };
    }
}
