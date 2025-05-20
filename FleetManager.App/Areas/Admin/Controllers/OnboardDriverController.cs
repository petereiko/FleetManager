using FleetManager.Business.Database.IdentityModels;
using FleetManager.Business.DataObjects;
using FleetManager.Business.Implementations.CompanyModule;
using FleetManager.Business.Interfaces.ComapyBranchModule;
using FleetManager.Business.Interfaces.CompanyModule;
using FleetManager.Business.Interfaces.CompanyOnboardingModule;
using FleetManager.Business.Interfaces.UserModule;
using FleetManager.Business.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FleetManager.App.Areas.Admin.Controllers
{
    [Area("Admin")]
    //[Authorize(Roles = "CompanyAdmin,CompanyOwner")]
    public class OnboardDriverController : Controller
    {
        private readonly ICompanyAdminService _adminService;
        private readonly IBranchService _branchService;
        private readonly ICompanyManagementService _companyService;
        private readonly UserManager<ApplicationUser> _userManager;

        private readonly IAuthUser _authUser;
        private readonly ILogger<OnboardDriverController> _logger;

        public OnboardDriverController(
            ICompanyAdminService adminService,
            IBranchService branchService,
            ICompanyManagementService companyService,
            IAuthUser authUser,
            ILogger<OnboardDriverController> logger,
            UserManager<ApplicationUser> userManager)
        {
            _adminService = adminService;
            _branchService = branchService;
            _companyService = companyService;
            _authUser = authUser;
            _logger = logger;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var admins = await _adminService.GetAllAdminsAsync();
            return View(admins);
        }


        [HttpGet]
        public async Task<IActionResult> RegisterAdmin()
        {
            try
            {
                var companyId = _authUser.CompanyId
                    ?? throw new InvalidOperationException("No CompanyId in user claims");

                // fetch all branches, then filter by company
                var allBranches = await _branchService.GetBranchesForCompanyAsync();
                var myBranches = allBranches.Where(b => b.CompanyId == companyId);

                // NEW: build a real SelectList
                ViewBag.Branches = new SelectList(myBranches,
                                                  dataValueField: "Id",
                                                  dataTextField: "Name");

                return View(new CompanyAdminOnboardingViewModel());
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "RegisterAdmin GET aborted");
                return RedirectToAction("Login", "Account", new { area = "" });
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterAdmin(CompanyAdminOnboardingViewModel model)
        {
            try
            {
                var companyId = _authUser.CompanyId
                    ?? throw new InvalidOperationException("No CompanyId in user claims");

                // reload branches asynchronously
                var branches = await _branchService.GetBranchesForCompanyAsync();
                ViewBag.Branches = branches
                    .Where(b => b.CompanyId == companyId)
                    .Select(b => new SelectListItem
                    {
                        Value = b.Id.ToString(),
                        Text = b.Name
                    })
                    .ToList();

                if (!ModelState.IsValid)
                    return View(model);

                var dto = new CompanyAdminOnboardingDto
                {
                    CompanyId = companyId,
                    CompanyBranchId = model.CompanyBranchId,
                    Email = model.Email,
                    PhoneNumber = model.PhoneNumber,
                    FirstName = model.FirstName,
                    LastName = model.LastName
                };

                var result = await _adminService.OnboardCompanyAdminAsync(dto, _authUser.UserId);
                if (!result.Success)
                {
                    ModelState.AddModelError("", result.Message);
                    return View(model);
                }

                TempData["SuccessMessage"] = result.Message;
                return RedirectToAction(nameof(RegisterAdmin));
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "RegisterAdmin POST aborted: user not fully authenticated");
                return RedirectToAction("Login", "Account", new { area = "" });
            }
        }





        //[HttpGet]
        //public async Task<IActionResult> RegisterAdmin()
        //{
        //    var user = await _userManager.FindByEmailAsync(_authUser.Email);
        //    if (user?.CompanyId == null)
        //    {
        //        _logger.LogWarning("User {Email} tried to onboard an admin but has no CompanyId.", _authUser.Email);
        //        return RedirectToAction("Login", "Account", new { area = "" });
        //    }

        //    await PopulateBranchesAsync(user.CompanyId.Value);

        //    return View(new CompanyAdminOnboardingViewModel());
        //}

        //[HttpPost, ValidateAntiForgeryToken]
        //public async Task<IActionResult> RegisterAdmin(CompanyAdminOnboardingDto model)
        //{
        //    var user = await _userManager.FindByEmailAsync(_authUser.Email);
        //    if (user?.CompanyId == null)
        //        return RedirectToAction("Login", "Account", new { area = "" });

        //    await PopulateBranchesAsync(user.CompanyId.Value);

        //    if (!ModelState.IsValid)
        //        return View(model);

        //    // map the fields you actually have in the VM
        //    var dto = new CompanyAdminOnboardingDto
        //    {
        //        CompanyId = user.CompanyId.Value,
        //        CompanyBranchId = model.CompanyBranchId,
        //        FirstName = model.FirstName,
        //        LastName = model.LastName,
        //        Email = model.Email,
        //        PhoneNumber = model.PhoneNumber
        //    };

        //    var result = await _adminService.OnboardCompanyAdminAsync(dto, _authUser.UserId);
        //    if (!result.Success)
        //    {
        //        ModelState.AddModelError("", result.Message);
        //        return View(model);
        //    }

        //    TempData["SuccessMessage"] = result.Message;
        //    return RedirectToAction(nameof(Index));
        //}


        private async Task PopulateBranchesAsync(long companyId)
        {
            var all = await _branchService.GetBranchesForCompanyAsync();
            var items = all
                .Where(b => b.CompanyId == companyId)
                .Select(b => new SelectListItem
                {
                    Value = b.Id.ToString(),
                    Text = b.Name
                })
                .ToList();

            ViewBag.Branches = items;
        }
    }
}



