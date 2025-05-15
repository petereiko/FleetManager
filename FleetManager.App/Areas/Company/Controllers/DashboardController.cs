using Microsoft.AspNetCore.Mvc;

namespace FleetManager.App.Areas.Company.Controllers
{
    [Area("Company")]
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
