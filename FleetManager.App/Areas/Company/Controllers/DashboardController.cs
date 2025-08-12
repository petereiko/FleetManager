using FleetManager.Business.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace FleetManager.App.Areas.Company.Controllers
{
    [Area("Company")]
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            CompanyOwnerDashboardViewModel viewModel = new CompanyOwnerDashboardViewModel
            {
                AdminCount = 10,
                BranchCount = 10,
                DriverCount = 30,
                VehicleCount = 40,
            };
            return View(viewModel);
        }
    }
}
