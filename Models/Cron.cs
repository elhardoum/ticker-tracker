using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;

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
                //var symbols = JsonConvert.DeserializeObject<List<Dictionary<string, Dictionary<string, string>>>>(data);
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
                //var symbols = JsonConvert.DeserializeObject<List<Dictionary<string, Dictionary<string, string>>>>(data);
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
    }
}
