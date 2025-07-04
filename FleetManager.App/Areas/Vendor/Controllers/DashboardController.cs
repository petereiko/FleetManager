using FleetManager.Business.Interfaces.UserModule;
using FleetManager.Business.Interfaces.VendorModule;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FleetManager.App.Areas.Vendor.Controllers
{
    [Area("Vendor")]
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly IVendorService _vendorService;
        private readonly IAuthUser _authUser;

        public DashboardController(IVendorService vendorService, IAuthUser authUser)
        {
            _vendorService = vendorService;
            _authUser = authUser;
        }

        public async Task<IActionResult> Index()
        {
            var model = await _vendorService.GetVendorDashboardAsync();

            if (model == null)
            {
                // Handle new vendor or error fallback
                TempData["Error"] = "No Vendor profile is linked to this account.";
                return RedirectToAction("Register", "Vendor");
            }

            return View(model);
        }
    }
}
