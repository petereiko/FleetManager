using FleetManager.Business.DataObjects.VendorDto;
using FleetManager.Business.Interfaces.RentalModule;
using FleetManager.Business.Interfaces.UserModule;
using Microsoft.AspNetCore.Mvc;

namespace FleetManager.App.Areas.Vendor.Controllers
{
    [Area("Vendor")]
    public class RentalController : Controller
    {
        private readonly IRentalService _rentalService;
        private readonly IAuthUser _authUser;

        public RentalController(IRentalService rentalService, IAuthUser authUser)
        {
            _rentalService = rentalService;
            _authUser = authUser;
        }

        // GET: /Vendor/Rental
        public async Task<IActionResult> Index()
        {
            var branchId = _authUser.CompanyBranchId
                ?? throw new InvalidOperationException("BranchId missing");
            var list = await _rentalService.GetRentalsForBranchAsync(branchId);
            return View(list);
        }

        // GET: /Vendor/Rental/Details/5
        public async Task<IActionResult> Details(long id)
        {
            var dto = await _rentalService.GetRentalByIdAsync(id);
            if (dto == null) return NotFound();
            return View(dto);
        }

        // GET: /Vendor/Rental/Apply?vendorId=123


        //public IActionResult Apply(long vendorId)
        //{
        //    var dto = new VehicleRentalOnboardingDto
        //    {
        //        VendorId = vendorId,
        //        CompanyBranchId = _authUser.CompanyBranchId
        //    };
        //    return View(dto);
        //}


        public async Task<IActionResult> Apply(long vendorId, long companyBranchId)
        {
            try
            {
                var vm = await _rentalService
                               .GetRentalApplyViewModelAsync(vendorId, companyBranchId);
                return View(vm);
            }
            catch (InvalidOperationException)
            {
                // You could also log here if desired
                return View("NotAuthorizedToApply");
            }
        }

        // POST: /Vendor/Rental/Apply
        //[HttpPost, ValidateAntiForgeryToken]
        //public async Task<IActionResult> Apply(VehicleRentalOnboardingDto dto)
        //{
        //    if (!ModelState.IsValid)
        //        return View(dto);

        //    var resp = await _rentalService.ApplyForRentalAsync(dto);
        //    if (!resp.Success)
        //    {
        //        ModelState.AddModelError("", resp.Message);
        //        return View(dto);
        //    }
        //    return RedirectToAction(nameof(Index));
        //}





        // GET: /Vendor/Rental/UploadAgreement/5


        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Apply(VehicleRentalApplyViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                // re‑populate the vendor/branch names and return the view
                vm = await _rentalService.GetRentalApplyViewModelAsync(
                    vm.VendorId, vm.CompanyBranchId);
                return View(vm);
            }

            // Here’s where we call your service method:
            var result = await _rentalService.ApplyForRentalAsync(vm.Rental);

            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                vm = await _rentalService.GetRentalApplyViewModelAsync(
                    vm.VendorId, vm.CompanyBranchId);
                return View(vm);
            }

            // on success, redirect back to the list or dashboard
            return RedirectToAction(nameof(Index));
        }





        public async Task<IActionResult> UploadAgreement(long id)
        {
            var dto = await _rentalService.GetRentalForAgreementAsync(id);
            if (dto == null) return NotFound();
            return View(dto);
        }

        // POST: /Vendor/Rental/UploadAgreement
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadAgreement(VehicleRentalAgreementDto dto)
        {
            if (!ModelState.IsValid)
                return View(dto);

            //var resp = await _rentalService.UpdateRentalAsync(dto);
            //if (!resp.Success)
            //{
            //    ModelState.AddModelError("", resp.Message);
            //    return View(dto);
            //}
            return RedirectToAction(nameof(Details), new { id = dto.Id });
        }

        // POST: /Vendor/Rental/Delete/5
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(long id)
        {
            var resp = await _rentalService.DeleteRentalAsync(id);
            if (!resp.Success)
                TempData["Error"] = resp.Message;
            return RedirectToAction(nameof(Index));
        }
    }
}
