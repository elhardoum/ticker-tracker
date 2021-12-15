using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace TickerTracker.Models
{
    public class DictQuote
    {
        public string Symbol;
        public decimal Movement;
        public decimal LastQuote;
        public decimal Quote;
        public DateTime Updated;
    }
}
