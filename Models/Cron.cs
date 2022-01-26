using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;
using Hangfire;
using System.Linq;
using Tweetinvi;
using Tweetinvi.Models;

namespace TickerTracker.Models
{
    public class Cron
    {
        // refresh stock/crypto list every hour
        private const int TICKER_LIST_REFRESH_TTL = 60 * 60;

        public async Task FetchStocks()
        {
            string optionName = "supported-stocks";

            var opt = await Util.getOption(optionName);

            if (null != opt && (DateTime.Now - opt.Updated).TotalSeconds <= TICKER_LIST_REFRESH_TTL)
                return;

            string data = await Util.getUrl("https://finnhub.io/api/v1/stock/symbol?exchange=US&token=" + Util.getEnv("FINNHUB_TOKEN"));

            if ( ! string.IsNullOrEmpty(data) )
            {
                var symbols = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(data);

                var prepared = new Dictionary<string, string>();

                symbols.ForEach(data =>
                {
                    if (!data.TryGetValue("description", out string desc))
                        return;

                    if (!data.TryGetValue("symbol", out string symbol))
                        return;

                    if (string.IsNullOrEmpty(symbol))
                        return;

                    prepared.Add(symbol.ToUpper(), ! string.IsNullOrEmpty(desc) ? desc : symbol);
                });

                if ( prepared.Count > 0 )
                    await Util.setOption(optionName, JsonConvert.SerializeObject(prepared));
            }
        }

        public async Task FetchCrypto()
        {
            string optionName = "supported-crypto";

            var opt = await Util.getOption(optionName);

            if (null != opt && (DateTime.Now - opt.Updated).TotalSeconds <= TICKER_LIST_REFRESH_TTL)
                return;

            string data = await Util.getUrl("https://api.coingecko.com/api/v3/coins/list");

            if (!string.IsNullOrEmpty(data))
            {
                var symbols = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(data);

                var prepared = new Dictionary<string, Dictionary<string, string>>();

                symbols.ForEach(data =>
                {
                    if (!data.TryGetValue("id", out string id))
                        return;

                    if (!data.TryGetValue("symbol", out string symbol))
                        return;

                    data.TryGetValue("name", out string name);

                    if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(symbol))
                        return;

                    prepared.Add(id, new Dictionary<string, string>{
                        { "symbol", symbol },
                        { "name", string.IsNullOrEmpty(name) ? symbol : name },
                    });
                });

                if (prepared.Count > 0)
                    await Util.setOption(optionName, JsonConvert.SerializeObject(prepared));
            }
        }

        [DisableConcurrentExecution(timeoutInSeconds: 10 * 60)]
        public async Task FetchQuotes()
        {
            var symbols = new List<Dictionary<string, string>>();

            await Database.Query(async (conn) =>
            {
                // select tickers updated longer than 1 hour ago
                String query = @"select max(q.Updated) as Updated, case
                        when max(q.Updated) is null then p.Symbol
                        when datediff(second, max(q.Updated), getdate()) >= 60*60 then p.Symbol
                        else ''
                    end as Symbol,
                        max(p.IsCrypto) as IsCrypto,
                        max(q.Quote) as LastQuote
                    from Quotes q
                    right join Portfolio p on p.Symbol = q.Symbol and p.Enabled = 1
                    group by p.Symbol
                    order by Updated desc";

                SqlCommand command = new SqlCommand(query, conn);

                try
                {
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (reader.Read())
                        {
                            var symbol = reader["Symbol"].ToString();

                            if ( ! string.IsNullOrEmpty(symbol) )
                            {
                                symbols.Add(new Dictionary<string, string>
                                {
                                    { "Symbol", symbol },
                                    { "IsCrypto", reader["IsCrypto"].ToString() },
                                    { "LastQuote", reader["LastQuote"].ToString() },
                                    { "LastQuoteUpdated", reader["Updated"].ToString() },
                                });
                            }
                        }
                    }
                }
                catch (SqlException e)
                {
                    Console.WriteLine("Tweets.findOne error: {0}, {1}", e.ToString(), e.Message);
                }
            });

            Console.WriteLine("fetch quotes: " + symbols.Count);

            if (0 == symbols.Count)
                return;

            var crypto = await Models.Util.getSupportedCrypto();

            Task.WaitAll(symbols.Select(item => Task.Run(async () =>
            {
                decimal? quote;

                if ( "1" == item["IsCrypto"] )
                {
                    var _data = crypto.ToList().Find(s => s.Value["symbol"].ToUpper() == item["Symbol"].ToUpper());

                    if (null == _data.Key)
                        return;

                    quote = await getCryptoQuote(_data.Key.ToString());
                } else
                {
                    quote = await getStockQuote(item["Symbol"]);
                }

                if ( null != quote )
                {
                    decimal LastQuote;
                    decimal.TryParse(item["LastQuote"], out LastQuote);

                    DateTime LastQuoteUpdated;
                    DateTime.TryParse(item["LastQuoteUpdated"], out LastQuoteUpdated);

                    DateTime Updated = DateTime.Now;

                    bool saved = await SaveQuote(item["Symbol"], (decimal) quote, Updated, LastQuote, LastQuoteUpdated);

                    if ( saved && LastQuote > 0 && DateTime.MinValue != LastQuoteUpdated )
                    {
                        // check if updated 1 hr ago or longer
                        if ( (Updated - LastQuoteUpdated).TotalSeconds >= 60 * 60 )
                        {
                            // Percent > 0 ? movement >= Percent : movement <= Percent
                            decimal Movement = ((100 * (decimal)quote) / (decimal)LastQuote) - 100;

                            if ( Movement != 0 )
                            {
                                await SaveTweetQuote(item["Symbol"], Movement, LastQuote, (decimal) quote, Updated);
                            }
                        }
                    }
                }
            })).ToArray());
        }

        private async Task<decimal?> getCryptoQuote( string id )
        {
            string data = await Util.getUrl($"https://api.coingecko.com/api/v3/simple/price?ids={id}&vs_currencies=usd");

            if (!string.IsNullOrEmpty(data))
            {
                dynamic values = JsonConvert.DeserializeObject(data);

                try
                {
                    return decimal.Parse(values[id].usd.ToString());
                } catch(Exception)
                {
                }
            }

            return null;
        }

        private async Task<decimal?> getStockQuote(string id)
        {
            string data = await Util.getUrl($"https://finnhub.io/api/v1/quote?symbol={id}&token=" + Util.getEnv("FINNHUB_TOKEN"));

            if (!string.IsNullOrEmpty(data))
            {
                dynamic values = JsonConvert.DeserializeObject(data);

                try
                {
                    return decimal.Parse(values.c.ToString());
                }
                catch (Exception)
                {
                }
            }

            return null;
        }

        private async Task<bool> SaveQuote(string Symbol, decimal Quote, DateTime Updated, decimal LastQuote, DateTime LastQuoteUpdated)
        {
            bool done = false;

            await Database.Query(async (conn) =>
            {
                bool hasLastUpdated = DateTime.MinValue != LastQuoteUpdated;

                String query = @$"insert into Quotes (Symbol, Quote, Updated, LastQuote{(hasLastUpdated ? ", LastQuoteUpdated" : "")})
                        values (@Symbol, @Quote, @Updated, @LastQuote{(hasLastUpdated ? ", @LastQuoteUpdated" : "")});";

                SqlCommand command = new SqlCommand(query, conn);

                command.Parameters.Add(new SqlParameter("@Symbol", Symbol));
                command.Parameters.Add(new SqlParameter("@Quote", Quote));
                command.Parameters.Add(new SqlParameter("@Updated", Updated));
                command.Parameters.Add(new SqlParameter("@LastQuote", LastQuote));

                if ( hasLastUpdated )
                    command.Parameters.Add(new SqlParameter("@LastQuoteUpdated", LastQuoteUpdated));

                try
                {
                    done = await command.ExecuteNonQueryAsync() > 0;
                }
                catch (SqlException e)
                {
                    Console.WriteLine("Cron.SaveQuote error: {0}, {1}", e.ToString(), e.Message);
                }
            });

            return done;
        }

        private async Task<bool> SaveTweetQuote(string Symbol, decimal Movement, decimal LastQuote, decimal Quote, DateTime Updated)
        {
            bool done = false;

            await Database.Query(async (conn) =>
            {
                String query = @"if exists ( select 1 from DictQuotes where Symbol=@Symbol )
                    update DictQuotes set Movement=@Movement, LastQuote=@LastQuote, Quote=@Quote, Updated=@Updated
                        where Symbol=@Symbol;
                else
                    insert into DictQuotes (Symbol, Movement, LastQuote, Quote, Updated)
                        values (@Symbol, @Movement, @LastQuote, @Quote, @Updated);";

                SqlCommand command = new SqlCommand(query, conn);

                command.Parameters.Add(new SqlParameter("@Symbol", Symbol));
                command.Parameters.Add(new SqlParameter("@Movement", Math.Round(Movement, 2)));
                command.Parameters.Add(new SqlParameter("@LastQuote", LastQuote));
                command.Parameters.Add(new SqlParameter("@Quote", Quote));
                command.Parameters.Add(new SqlParameter("@Updated", Updated));

                try
                {
                    done = await command.ExecuteNonQueryAsync() > 0;
                }
                catch (SqlException e)
                {
                    Console.WriteLine("Cron.SaveTweetQuote error: {0}, {1}", e.ToString(), e.Message);
                }
            });

            return done;
        }

        [DisableConcurrentExecution(timeoutInSeconds: 10 * 60)]
        public async Task Tweet()
        {
            var items = new List<TweetTask>();
            Console.WriteLine(".init :: {0}", items.Count);

            await Database.Query(async (conn) =>
            {
                String query = @"select top 10 t.*, p.*, t.Updated as QuoteUpdated, u.Handle, u.Secret, u.Token from DictQuotes t
                    join Portfolio p on p.Enabled = 1 and p.Symbol = t.Symbol and (
                        p.LastTweetedQuoteTimestamp is null
                        or t.Updated > p.LastTweetedQuoteTimestamp
                    )
                    join Users u on u.Id = p.UserId
                    where datediff(second, t.Updated, getdate()) <= 30*60
                        and (
                            ( [Percent] < 0 and Movement <= [Percent] )
                            or ( [Percent] > 0 and Movement >= [Percent] )
                        ) and (
                            select case
                                when datediff(second, max(Created), getdate()) <= 60*60 then max(Created)
                                else null
                            end as Created from Tweets where PortfolioId = p.Id group by PortfolioId
                        ) is null";

                SqlCommand command = new SqlCommand(query, conn);

                try
                {
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (reader.Read())
                        {
                            var symbol = reader["Symbol"].ToString();

                            if ( ! string.IsNullOrEmpty(symbol) )
                            {
                                items.Add(new TweetTask
                                {
                                    portfolioItem = PortfolioItems.parse(reader),
                                    quote = new DictQuote
                                    {
                                        Symbol = reader["Symbol"].ToString(),
                                        Movement = decimal.Parse(reader["Movement"].ToString()),
                                        LastQuote = decimal.Parse(reader["LastQuote"].ToString()),
                                        Quote = decimal.Parse(reader["Quote"].ToString()),
                                        Updated = DateTime.Parse(reader["QuoteUpdated"].ToString()),
                                    },
                                    handle = reader["Handle"].ToString(),
                                    secret = reader["Secret"].ToString(),
                                    token = reader["Token"].ToString(),
                                });
                            }
                        }
                    }
                }
                catch (SqlException e)
                {
                    Console.WriteLine("Tweets.findOne error: {0}, {1}", e.ToString(), e.Message);
                }
            });

            Console.WriteLine("TWEET>> {0}", items.Count);

            if (0 == items.Count)
                return;

            var crypto = await Models.Util.getSupportedCrypto();

            Task.WaitAll(items.Select(item => Task.Run(async () =>
            {
                var Tweet = new Tweet
                {
                    Text = formatText(item), // string
                    Url = "", // string
                    PortfolioId = item.portfolioItem.Id, // long
                    Created = DateTime.Now, // DateTime
                };

                Console.WriteLine( $"{item.quote.Symbol} / mv: {item.quote.Movement}" );
                Console.WriteLine( Tweet.Text );

                var oldValue = item.portfolioItem.LastTweetedQuoteTimestamp;

                item.portfolioItem.LastTweetedQuoteTimestamp = item.quote.Updated;

                if ( await item.portfolioItem.Save() )
                {
                    if ( ! await Tweet.Save() )
                    {
                        item.portfolioItem.LastTweetedQuoteTimestamp = oldValue;
                        await item.portfolioItem.Save();
                    }

                }
            })).ToArray());
        }

        public string formatText( TweetTask item )
        {
            string template = item.portfolioItem.TweetText;

            if ( string.IsNullOrEmpty(template)) // use default
            {
                template = "${TICKER} has moved {MOVEMENT}% over the past hour. #{TICKER}";
            }

            while (template.Contains("{TICKER}")) template = template.Replace("{TICKER}", item.quote.Symbol.ToUpper());
            while (template.Contains("{MOVEMENT}")) template = template.Replace("{MOVEMENT}", item.quote.Movement.ToString());

            return template;
        }

        [DisableConcurrentExecution(timeoutInSeconds: 10 * 60)]
        public async Task PublishTweets()
        {
            var items = new List<Tweet>();
            var tokens = new Dictionary<long, string>();
            var secrets = new Dictionary<long, string>();

            Console.WriteLine("publish.init :: {0}", items.Count);

            await Database.Query(async (conn) =>
            {
                // skip stale tweets, those older than 1 hour and not published successfully
                String query = @"select top 10 t.*, p.Symbol, p.IsCrypto, p.[Percent], p.UserId, u.Token, u.Secret from Tweets t
                    join Portfolio p on t.PortfolioId = p.Id
                    join Users u on u.Id = p.UserId
                    where ([Url] is null or [Url] = '')
                        and datediff(second, t.Created, getdate()) <= 60*60";

                SqlCommand command = new SqlCommand(query, conn);

                try
                {
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (reader.Read())
                        {
                            if ( ! string.IsNullOrEmpty(reader["Text"].ToString()) )
                            {
                                var tweet = Tweets.parse(reader);

                                if ( ! tokens.ContainsKey(tweet.PortfolioItemMin.UserId) )
                                {
                                    tokens.Add(tweet.PortfolioItemMin.UserId, reader["token"].ToString());
                                }

                                if ( ! secrets.ContainsKey(tweet.PortfolioItemMin.UserId) )
                                {
                                    secrets.Add(tweet.PortfolioItemMin.UserId, reader["secret"].ToString());
                                }

                                items.Add(tweet);
                            }
                        }
                    }
                }
                catch (SqlException e)
                {
                    Console.WriteLine("Tweets.findOne error: {0}, {1}", e.ToString(), e.Message);
                }
            });

            Console.WriteLine("post>> {0}", items.Count);

            if (0 == items.Count)
                return;

            Task.WaitAll(items.Select(item => Task.Run(async () =>
            {
                Console.WriteLine("Publish tweet " + item.Text );

                var userClient = new TwitterClient(
                    Models.Util.getEnv("TWITTER_CONSUMER_KEY"),
                    Models.Util.getEnv("TWITTER_CONSUMER_SECRET"),
                    tokens[item.PortfolioItemMin.UserId],
                    secrets[item.PortfolioItemMin.UserId]);

                ITweet tweet = null;

                try
                {
                    tweet = await userClient.Tweets.PublishTweetAsync(item.Text);
                } catch (Exception e) {
                    Console.WriteLine(e.Message);
                }

                if ( null != tweet && tweet.Id > 0)
                {
                    item.Url = "https://twitter.com/x/status/" + tweet.Id;
                    await item.Save();
                    Console.WriteLine("Tweet published " + tweet.Id);
                }
            })).ToArray());
        }
    }
}
