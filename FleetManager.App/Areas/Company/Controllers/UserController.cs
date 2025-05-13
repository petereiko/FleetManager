using FleetManager.Business.ViewModels;
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


        [HttpPost]
        public IActionResult Create(CompanyRegistrationViewModel model)
        {
            return View();
        }
    }
}
