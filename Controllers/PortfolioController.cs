using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Linq;

namespace TickerTracker.Controllers
{
    public class PortfolioController : Controller
    {
        public async Task<IActionResult> Index()
        {
            ViewData["items"] = (await Models.PortfolioItems.findByField("UserId", ((Models.User)HttpContext.Items["user"]).Id.ToString()))
                .OrderByDescending(o => o.Id).ToList();

            return View();
        }

        public async Task<IActionResult> Update(long? editId = 0) => await Create(editId);

        public async Task<IActionResult> Create(long? editId = 0)
        {
            Models.PortfolioItem editItem = null;

            if (editId > 0)
            {
                editItem = new Models.PortfolioItem
                {
                    Id = (long)editId
                };

                if (!await editItem.Load())
                    return Redirect(Url.Action("Index", "Portfolio", null, Request.Scheme));

                if (editItem.UserId != ((Models.User)HttpContext.Items["user"]).Id)
                    return Redirect(Url.Action("Index", "Portfolio", null, Request.Scheme));
            }

            ViewData["editItem"] = null != editItem && editItem.Id > 0 ? editItem : null;

            if ("POST" == Request.Method)
            {
                bool complete = true;
                complete = complete && Request.Form.TryGetValue("ticker", out StringValues ticker);
                complete = complete && Request.Form.TryGetValue("percentage", out StringValues percentage);
                complete = complete && Request.Form.TryGetValue("tweet", out StringValues tweet);
                Request.Form.TryGetValue("enabled", out StringValues enabled);

                if (complete)
                {
                    var stocks = await getSupportedStocks();
                    var crypto = await getSupportedCrypto();
                    List<string> errors = new List<string>();
                    bool isStock = false;
                    bool isCrypto = false;
                    double percent = 0;

                    if (0 == ticker.Count || string.IsNullOrEmpty(ticker[0].Trim()))
                    {
                        errors.Add("Please enter a valid ticker symbol.");
                    } else
                    {
                        // check if symbol is a valid stock/etf
                        isStock = stocks.ContainsKey(ticker[0].Trim().ToUpper());

                        if (!isStock)
                        { // symbol is not a stock, check if symbol is in crypto dict
                            foreach (var x in crypto.Values)
                            {
                                if (x.TryGetValue("symbol", out string symbol))
                                {
                                    if (symbol.ToString().ToUpper() == ticker[0].Trim().ToUpper())
                                    {
                                        isCrypto = true;
                                        break;
                                    }
                                }
                            }
                        }

                        if (!isStock && !isCrypto)
                        {
                            errors.Add("Symbol not supported.");
                        }
                    }

                    if (0 == percentage.Count || string.IsNullOrEmpty(percentage[0].Trim()))
                    {
                        errors.Add("Please enter a movement percentage.");
                    }
                    else if (!double.TryParse(percentage[0].Trim(), out percent))
                    {
                        errors.Add("Please enter a valid movement percentage.");
                    }
                    else if (percent == 0 || percent > 100 || percent < -100)
                    {
                        errors.Add("Please enter a valid movement percentage.");
                    }

                    if (tweet.Count > 0 && percentage[0].Trim().Length > 500)
                    {
                        errors.Add("Tweet text cannot be longer than 500 characters. Remember, a tweet cannot exceed 280 characters to go through.");
                    }

                    if (0 == errors.Count)
                    {
                        if (null != editItem && editItem.Id > 0)
                        {
                            editItem.Symbol = ticker[0].Trim().ToUpper();
                            editItem.IsCrypto = isCrypto ? 1 : 0;
                            editItem.Percent = percent;
                            editItem.TweetText = tweet;
                            editItem.Enabled = enabled.Count > 0 ? 1 : 0;
                            editItem.Updated = DateTime.Now;

                            if (await editItem.Save())
                            {
                                return Redirect(Url.Action("Index", "Portfolio", null, Request.Scheme));
                            }
                        }
                        else
                        {
                            // proceed with insertion
                            var item = new Models.PortfolioItem
                            {
                                UserId = ((Models.User)HttpContext.Items["user"]).Id,
                                Symbol = ticker[0].Trim().ToUpper(),
                                IsCrypto = isCrypto ? 1 : 0,
                                Enabled = enabled.Count > 0 ? 1 : 0,
                                Percent = percent,
                                TweetText = tweet,
                            };

                            if (await item.Save())
                            {
                                return Redirect(Url.Action("Index", "Portfolio", null, Request.Scheme));
                            }
                        }

                        errors.Add("Internal server error, entry could not be saved. Please try again.");
                    }

                    ViewData["errors"] = errors;
                }
            }

            return View("~/Views/Portfolio/Create.cshtml");
        }

        public async Task<IActionResult> Delete(long deleteId)
        {
            var item = new Models.PortfolioItem
            {
                Id = (long) deleteId
            };

            if (!await item.Load())
                return Redirect(Url.Action("Index", "Portfolio", null, Request.Scheme));

            if (item.UserId != ((Models.User)HttpContext.Items["user"]).Id)
                return Redirect(Url.Action("Index", "Portfolio", null, Request.Scheme));

            await item.Delete();

            return Redirect(Url.Action("Index", "Portfolio", null, Request.Scheme));
        }

        private async Task<Dictionary<string, string>> getSupportedStocks()
        {
            var stocksRaw = await Models.Util.getOption("supported-stocks");
            var stocks = new Dictionary<string, string>();

            if (null != stocksRaw)
            {
                stocks = JsonConvert.DeserializeObject<Dictionary<string, string>>(stocksRaw.Value);
            }

            return stocks;
        }

        private async Task<Dictionary<string, Dictionary<string, string>>> getSupportedCrypto()
        {
            var cryptoRaw = await Models.Util.getOption("supported-crypto");
            var crypto = new Dictionary<string, Dictionary<string, string>>();

            if (null != cryptoRaw)
            {
                crypto = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(cryptoRaw.Value);
            }

            return crypto;
        }
    }
}
