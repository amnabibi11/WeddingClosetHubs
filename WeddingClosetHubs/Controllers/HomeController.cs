using Microsoft.AspNetCore.Mvc;

namespace WeddingClosetHubs.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
