using FleetManager.Business.DataObjects.VendorDto;
using FleetManager.Business.Interfaces.VendorModule;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FleetManager.App.Areas.Vendor.Controllers
{
    [Area("Vendor")]
    public class VendorController : Controller
    {
        private readonly IVendorService _vendorService;
        private readonly ILogger<VendorController> _logger;

        public VendorController(
            IVendorService vendorService,
            ILogger<VendorController> logger)
        {
            _vendorService = vendorService;
            _logger = logger;
        }

        public async Task<IActionResult> Index(string? search, int? category)
        {
            ViewBag.Categories = _vendorService.GetVendorCategoryOptions();
            var vendors = await _vendorService.GetVendorsAsync( search, category);
            return View(vendors);
        }

        [HttpGet]
        public async Task<IActionResult> Register()
        {
            ViewBag.Categories = _vendorService.GetVendorCategoryOptions(); 
            var states = await _vendorService.GetAllStatesAsync();
            ViewBag.States = states.Select(s => new SelectListItem
            {
                Value = s.Id.ToString(),
                Text = s.Name
            }).ToList();

            return View(new VendorOnboardingDto());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(VendorOnboardingDto dto)
        {
            ViewBag.Categories = _vendorService.GetVendorCategoryOptions();
            var states = await _vendorService.GetAllStatesAsync();
            ViewBag.States = states.Select(s => new SelectListItem
            {
                Value = s.Id.ToString(),
                Text = s.Name
            }).ToList();

            if (!ModelState.IsValid)
                return View(dto);

            var result = await _vendorService.OnboardVendorAsync(dto);
            if (!result.Success)
            {
                ModelState.AddModelError("", result.Message);
                return View(dto);
            }

            TempData["SuccessMessage"] = result.Message;
            return RedirectToAction("Login", "Account", new { area = "" });
        }

        //[HttpGet]
        //public async Task<IActionResult> Edit(long id)
        //{
        //    var vendor = await _vendorService.GetVendorByIdAsync(id);
        //    if (vendor == null) return NotFound();

        //    ViewBag.Categories = _vendorService.GetVendorCategoryOptions();
        //    ViewBag.Services = _vendorService.GetVendorServiceOptions();
        //    return View(vendor);
        //}

        //[HttpPost, ValidateAntiForgeryToken]
        //public async Task<IActionResult> Edit(VendorDto dto)
        //{
        //    ViewBag.Categories = _vendorService.GetVendorCategoryOptions();
        //    ViewBag.Services = _vendorService.GetVendorServiceOptions();

        //    if (!ModelState.IsValid)
        //        return View(dto);

        //    var result = await _vendorService.UpdateVendorAsync(dto);
        //    if (!result.Success)
        //    {
        //        ModelState.AddModelError("", result.Message);
        //        return View(dto);
        //    }

        //    TempData["SuccessMessage"] = "Vendor updated successfully.";
        //    return RedirectToAction(nameof(Index));
        //}

        //[HttpPost, ValidateAntiForgeryToken]
        //public async Task<IActionResult> Delete(long id)
        //{
        //    var result = await _vendorService.DeleteVendorAsync(id);
        //    TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        //    return RedirectToAction(nameof(Index));
        //}

        //[HttpGet]
        public async Task<IActionResult> Details(long id)
        {
            var vendor = await _vendorService.GetVendorByIdAsync(id);
            if (vendor == null) return NotFound();

            return View(vendor);
        }     

    }
}

