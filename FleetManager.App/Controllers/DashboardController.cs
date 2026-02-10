using FleetManager.App.Models;
using FleetManager.Business.Interfaces.DriverDashboardModule;
using FleetManager.Business.Interfaces.UserModule;
using FleetManager.Business.ViewModels;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace FleetManager.App.Controllers
{
    [Authorize(Policy = "DriverWeb")]
    public class DashboardController : Controller
    {
        private readonly IDriverDashboardService _dashboardService;

        public DashboardController(IDriverDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        //public async Task<IActionResult> Index()
        //{
        //    var resp = await _dashboardService.GetDriverDashboardAsync();
        //    if (!resp.Success) return View("Error", resp.Message);
        //    return View(resp.Result);
        //}

        public async Task<IActionResult> Index()
        {
            var resp = await _dashboardService.GetDriverDashboardAsync();

            if (!resp.Success)
            {
                return View("Error", new ErrorViewModel
                {
                    RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                    Message = resp.Message  // Add this property if it doesn't exist
                });
            }

            return View(resp.Result);
        }

        public IActionResult Details()
        {
            return View();
        }
    }
}
