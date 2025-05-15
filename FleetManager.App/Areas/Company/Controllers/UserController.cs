using FleetManager.Business.Database.IdentityModels;
using FleetManager.Business.Interfaces.CompanyModule;
using FleetManager.Business.Interfaces.UserModule;
using FleetManager.Business.UtilityModels;
using FleetManager.Business.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace FleetManager.App.Areas.Company.Controllers
{
    [Area("Company")]
    public class UserController : Controller
    {
        private readonly ICompanyManagementService _companyService;
        private readonly UserManager<ApplicationUser> _userManager;



        public UserController(ICompanyManagementService companyService, UserManager<ApplicationUser> userManager)
        {
            _companyService = companyService;
            _userManager = userManager;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new CompanyRegistrationViewModel());
        }


        [HttpPost]
        public async Task<IActionResult> Create(CompanyRegistrationViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var result = await _companyService.OnboardCompany(model);

            if (result.Success)
            {
                
                TempData["SuccessMessage"] = "Company registered! Please check your email to confirm.";
                return RedirectToAction(nameof(ConfirmEmailSent));
            }
            else
            {
                ModelState.AddModelError("", result.Message);
                return View(model);
            }
        }

        public async Task<IActionResult> ConfirmEmail(string encodedToken, string userId)
        {

            MessageResponse response = _companyService.ConfirmEmail(encodedToken, userId).Result;
            if (response.Success)
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)

                    return NotFound("User not found");
                return RedirectToAction("ForcePasswordChange", "User", new { userId = user.Id });
            }
            else
            {
                ViewBag.ErrorMessage = response.Message;
                return View("Error");
            }
            
        }

        [HttpGet]
        public IActionResult ConfirmEmailSent()
        {
            
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> ForcePasswordChange(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound();

            var model = new ChangeAdminPasswordViewModel
            {
                UserId = user.Id,
                Email = user.Email,
                
            };

            return View(model); 
        }

        [HttpPost]
        public async Task<IActionResult> ForcePasswordChange(ChangeAdminPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null) return NotFound();

            var removePassword = await _userManager.RemovePasswordAsync(user);
            if (!removePassword.Succeeded)
            {
                ModelState.AddModelError("", "Could not reset password.");
                return View(model);
            }

            var addPassword = await _userManager.AddPasswordAsync(user, model.NewPassword);
            if (!addPassword.Succeeded)
            {
                ModelState.AddModelError("", addPassword.Errors.First().Description);
                return View(model);
            }

            user.IsFirstLogin = false;
            user.LastLoginDate = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            TempData["SuccessMessage"] = "Password changed successfully.";

            return RedirectToAction("Login", "Account");
        }


    }
}
