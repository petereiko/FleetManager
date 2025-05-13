using Microsoft.AspNetCore.Mvc;

namespace FleetManager.App.Areas.Company.Controllers
{
    [Area("Company")]
    public class UserController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

       
    }
}
