using System;
using Microsoft.AspNetCore.Mvc;

namespace TickerTracker.Controllers
{
    public class PortfolioController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Create()
        {
            return View();
        }
    }
}
