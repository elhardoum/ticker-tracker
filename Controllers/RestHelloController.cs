using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;

namespace TickerTracker.Controllers
{
    public class RestHelloController : Controller
    {
        public IActionResult Index()
        {
            var user = new Dictionary<string, string>(){
                {"first_name", "John"},
                {"last_name", "Doe"},
                {"email_address", "john-doe@example.com"}
            };

            return Json(user);
        }
    }
}
