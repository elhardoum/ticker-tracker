using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace TickerTracker.Models
{
    public class TweetTask
    {
        public DictQuote quote;
        public PortfolioItem portfolioItem;
        public string handle;
        public string secret;
        public string token;
    }
}
