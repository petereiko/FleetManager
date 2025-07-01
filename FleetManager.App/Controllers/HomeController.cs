using System.Diagnostics;
using FleetManager.App.Models;
using FleetManager.Business.Implementations.EmailModule;
using FleetManager.Business.Interfaces.VehicleModule;
using Hangfire;
using Microsoft.AspNetCore.Mvc;

namespace FleetManager.App.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IAdminVehicleService _adminVehicleService;
        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            //// Fire-and-forget Hangfire job
            //BackgroundJob.Enqueue<IAdminVehicleService>(service => service.LoadModels());

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }


        // GET /Home/StatusCode/401  or /Home/StatusCode/403 or /Home/StatusCode/404
        [HttpGet("Home/StatusCode/{code}")]
        public IActionResult StatusCodeHandler(int code)
        {
            switch (code)
            {
                case 401:
                case 403:
                    return View("SessionExpired");    // Views/Home/SessionExpired.cshtml
                case 404:
                    return View("NotFound");         // Views/Home/NotFound.cshtml
                default:
                    return View("GenericStatus", code); // Views/Home/GenericStatus.cshtml
            }
        }

    }
}
