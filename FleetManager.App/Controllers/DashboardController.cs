using FleetManager.Business.Interfaces.UserModule;
using FleetManager.Business.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FleetManager.App.Controllers
{

    [Authorize]
    public class DashboardController : Controller
    {
        //private readonly IAuthUser _authUser;
        //public DashboardController(IAuthUser authUser): base(authUser)
        //{
        //    _authUser = authUser;
        //}

        public IActionResult Index()
        {
            
            //var obj = _authUser;

            return View();
        }

        public IActionResult Details()
        {
            return View();
        }
    }
}
