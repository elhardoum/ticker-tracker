using Microsoft.AspNetCore.Mvc;

namespace TickerTracker.Controllers
{
    public class HttpErrorController : Controller
    {
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult _404()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult _401()
        {
            return View();
        }
    }
}
