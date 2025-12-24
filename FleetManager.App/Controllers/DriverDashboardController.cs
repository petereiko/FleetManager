using FleetManager.Business.Interfaces.DriverDashboardModule;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FleetManager.App.Controllers
{
    
    [Authorize(Roles = "Driver")]
    public class DriverDashboardController : Controller
    {
        private readonly IDriverDashboardService _dashboardService;

        public DriverDashboardController(IDriverDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        public async Task<IActionResult> Index()
        {
            var resp = await _dashboardService.GetDriverDashboardAsync();
            if (!resp.Success) return View("Error", resp.Message);
            return View(resp.Result);
        }
    }
}
