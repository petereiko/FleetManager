using FleetManager.Business.Interfaces.DriverDashboardModule;
using FleetManager.Business.Interfaces.UserModule;
using FleetManager.Business.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FleetManager.App.Controllers
{
    public class DashboardController : Controller
    {
        private readonly IDriverDashboardService _dashboardService;

        public DashboardController(IDriverDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        public async Task<IActionResult> Index()
        {
            var resp = await _dashboardService.GetDriverDashboardAsync();
            if (!resp.Success) return View("Error", resp.Message);
            return View(resp.Result);
        }

        public IActionResult Details()
        {
            return View();
        }
    }
}
