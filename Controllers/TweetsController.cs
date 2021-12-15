using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Linq;

namespace TickerTracker.Controllers
{
    public class TweetsController : Controller
    {
        public async Task<IActionResult> Index()
        {
            ViewData["items"] = (await Models.Tweets.findByUserId( ((Models.User)HttpContext.Items["user"]).Id ))
                .OrderByDescending(o => o.Id).ToList();

            return View();
        }

        public async Task<IActionResult> ByPortfolio(long id)
        {
            var portfolio = new Models.PortfolioItem { Id = id };
            await portfolio.Load();

            if (((Models.User)HttpContext.Items["user"]).Id != portfolio.UserId)
            {
                return Redirect(Url.Action("Index", "Tweets", null, Request.Scheme));
            }

            ViewData["items"] = (await Models.Tweets.findByField("PortfolioId", portfolio.Id.ToString()))
                .OrderByDescending(o => o.Id).ToList();

            return View("~/Views/Tweets/Index.cshtml");
        }

        public async Task<IActionResult> Delete(long deleteId)
        {
            var item = new Models.Tweet
            {
                Id = (long) deleteId
            };

            if (!await item.Load())
                return Redirect(Url.Action("Index", "Tweets", null, Request.Scheme));

            // @todo check user
            return Json("null");

            //if (item.UserId != ((Models.User)HttpContext.Items["user"]).Id)
            //    return Redirect(Url.Action("Index", "Tweets", null, Request.Scheme));

            await item.Delete();

            return Redirect(Url.Action("Index", "Tweets", null, Request.Scheme));
        }
    }
}
