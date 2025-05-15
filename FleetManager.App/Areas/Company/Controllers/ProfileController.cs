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
                LogoUrl = company.LogoUrl
                 
            };

            ViewBag.States = new SelectList(stateDtos, "Id", "Name", model.StateId);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditCompanyViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var response = await _companyService.EditCompanyProfile(model);

            if (!response.Success)
            {
                ModelState.AddModelError("", response.Message);
                return View(model);
            }

            TempData["SuccessMessage"] = "Profile updated successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}
