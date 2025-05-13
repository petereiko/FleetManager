using Microsoft.AspNetCore.Mvc;

namespace FleetManager.App.Controllers
{
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
