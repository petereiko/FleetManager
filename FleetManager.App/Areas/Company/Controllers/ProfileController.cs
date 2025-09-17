using FleetManager.Business.Implementations.CompanyModule;
using FleetManager.Business.Interfaces.CompanyModule;
using FleetManager.Business.Interfaces.UserModule;
using FleetManager.Business.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FleetManager.App.Areas.Company.Controllers
{
    [Area("Company")]
    public class ProfileController : Controller
    {
        private readonly ICompanyManagementService _companyService;
        private readonly IAuthUser _authUser;

        public ProfileController(ICompanyManagementService companyService, IAuthUser authUser)
        {
            _companyService = companyService;
            _authUser = authUser;
        }


        public async Task<IActionResult> Index()
        {
            var company = await _companyService.GetCompanyProfile();
            if (company == null)
            {
                return NotFound("Company profile not found.");
            }

            return View(company);
        }

        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            var company = await _companyService.GetCompanyProfile();
            if (company == null)
            {
                return NotFound("Company data not found.");
            }

            var stateDtos = await _companyService.GetAllStatesAsync();

            var model = new EditCompanyViewModel
            {
                StateId = stateDtos.FirstOrDefault(s => s.Name == company.State)?.Id,
                Name = company.Name,
                RegistrationNumber = company.RegistrationNumber,
                Address = company.Address,
                DateOfIncorporation = company.DateOfIncorporation,
                State = company.State,
                Email = company.Email,
                PhoneNumber = company.PhoneNumber,
                ContactPersonName = company.ContactPersonName,
                ContactPersonPhone = company.ContactPersonPhone,
                ContactPersonEmail = company.ContactPersonEmail,
                Website = company.Website,

                // Important: preserve the currently saved logo so the form can show it on first load
                // If your view-model uses LogoUrl instead of ExistingLogoUrl, assign that property instead.
                ExistingLogoUrl = company.LogoUrl
            };

            ViewBag.States = new SelectList(stateDtos, "Id", "Name", model.StateId);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditCompanyViewModel model)
        {
            // Always ensure the states list is available if we need to re-render the view
            var stateDtos = await _companyService.GetAllStatesAsync();
            ViewBag.States = new SelectList(stateDtos, "Id", "Name", model.StateId);

            if (!ModelState.IsValid)
            {
                // If the view expects ExistingLogoUrl to display the preview on validation errors,
                // make sure it's populated. If your form includes a hidden field for ExistingLogoUrl,
                // it will already be present in `model.ExistingLogoUrl`.
                return View(model);
            }

            var response = await _companyService.EditCompanyProfile(model);

            if (!response.Success)
            {
                // keep the existing logo preview if present (model.ExistingLogoUrl should already contain it)
                ModelState.AddModelError(string.Empty, response.Message);
                return View(model);
            }

            TempData["SuccessMessage"] = "Profile updated successfully.";
            return RedirectToAction(nameof(Index));
        }

    }
}