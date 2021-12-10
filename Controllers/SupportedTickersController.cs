using System;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace TickerTracker.Controllers
{
    public class SupportedTickersController : Controller
    {
        public async Task<IActionResult> Stocks()
        {
            var stocksRaw = await Models.Util.getOption("supported-stocks");
            var stocks = new Dictionary<string, string>();

            if ( null != stocksRaw )
			{
                stocks = JsonConvert.DeserializeObject<Dictionary<string, string>>(stocksRaw.Value);
            }

            ViewData["stocks"] = stocks;

            return View();
        }

        public async Task<IActionResult> Crypto()
        {
            var cryptoRaw = await Models.Util.getOption("supported-crypto");
            var crypto = new Dictionary<string, Dictionary<string, string>>();

            if ( null != cryptoRaw )
            {
                crypto = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(cryptoRaw.Value);
            }

            ViewData["crypto"] = crypto;

            return View();
        }
    }
}
