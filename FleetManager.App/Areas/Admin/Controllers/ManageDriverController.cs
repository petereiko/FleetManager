using FleetManager.Business;
using FleetManager.Business.DataObjects;
using FleetManager.Business.DataObjects.VehicleDto;
using FleetManager.Business.Enums;
using FleetManager.Business.Interfaces.ComapyBranchModule;
using FleetManager.Business.Interfaces.ManageDriverModule;
using FleetManager.Business.Interfaces.UserModule;
using FleetManager.Business.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FleetManager.App.Areas.Admin.Controllers
{

    [Area("Admin")]
    public class ManageDriverController : Controller
    {
        private readonly IManageDriverService _driverService;
        private readonly IBranchService _branchService;
        private readonly IAuthUser _authUser;
        private readonly ILogger<ManageDriverController> _logger;

        public ManageDriverController(
            IManageDriverService driverService,
            IBranchService branchService,
            IAuthUser authUser,
            ILogger<ManageDriverController> logger)
        {
            _driverService = driverService;
            _branchService = branchService;
            _authUser = authUser;
            _logger = logger;
        }

        // ─── INDEX: List drivers ───────────────────────────────────────────────
        public async Task<IActionResult> Index(int page = 1, int pageSize = 12)
        {
            try
            {
                // 1) determine roles
                var roles = (_authUser.Roles ?? "")
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(r => r.Trim());
                bool isGlobal = roles.Contains("Company Owner") || roles.Contains("Super Admin");

                // 2) get queryable from service
                //    you’ll need to expose IQueryable<DriverListItemDto> in your service
                var query = _driverService.QueryDriversForBranch(isGlobal ? null : _authUser.CompanyBranchId);

                // 3) total count
                var total = await query.CountAsync();

                // 4) pull only this page
                var list = await query
                    .OrderBy(d => d.FullName)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                // 5) build VM
                var vm = new DriverIndexViewModel
                {
                    Drivers = list,
                    IsGlobal = isGlobal,
                    Pagination = new PaginationDto
                    {
                        CurrentPage = page,
                        PageSize = pageSize,
                        TotalItems = total
                    }
                };

                return View(vm);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading driver list");
                return View("Error");
            }
        }

        // ─── DETAILS ────────────────────────────────────────────────────────────
        public async Task<IActionResult> Details(long id)
        {
            try
            {
                var dto = await _driverService.GetDriverByIdAsync(id);
                if (dto == null) return NotFound();
                return View(dto);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching driver details for Id {DriverId}", id);
                return View("Error");
            }
        }

        // ─── CREATE (Onboard) ───────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            try
            {
                var vm = new DriverOnboardViewModel();
                // Determine if global (owner/super) or branch‐admin
                var roles = (_authUser.Roles ?? "")
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(r => r.Trim());
                bool isGlobal = roles.Contains("Company Owner") || roles.Contains("Super Admin");

                if (isGlobal)
                {
                    var branches = await _branchService.GetBranchesForCompanyAsync();
                    vm.Branches = branches
                        .Select(b => new SelectListItem(b.Name, b.Id.ToString()));
                }
                else
                {
                    // single branch
                    var myBranchId = _authUser.CompanyBranchId ?? 0;
                    var branch = (await _branchService.GetBranchesForCompanyAsync())
                                 .FirstOrDefault(b => b.Id == myBranchId);
                    vm.Branches = new[]
                    {
                    new SelectListItem(branch?.Name ?? "Unknown", myBranchId.ToString())
                };
                    vm.CompanyBranchId = myBranchId;
                }

                // Populate all your dropdowns
                vm.Genders = EnumHelper.ToSelectList<Gender>();
                vm.EmploymentStatuses = EnumHelper.ToSelectList<EmploymentStatus>();
                vm.ShiftStatuses = EnumHelper.ToSelectList<ShiftStatus>();
                vm.LicenseCategories = EnumHelper.ToSelectList<LicenseCategory>();

                return View(vm);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DriverOnboardViewModel vm)
        {
            try
            {
                // repopulate dropdowns on POST
                var branches = await _branchService.GetBranchesForCompanyAsync();
                vm.Branches = branches.Select(b => new SelectListItem(b.Name, b.Id.ToString()));
                vm.Genders = EnumHelper.ToSelectList<Gender>();
                vm.EmploymentStatuses = EnumHelper.ToSelectList<EmploymentStatus>();
                vm.ShiftStatuses = EnumHelper.ToSelectList<ShiftStatus>();
                vm.LicenseCategories = EnumHelper.ToSelectList<LicenseCategory>();

                if (!ModelState.IsValid)
                    return View(vm);

                // map to onboarding DTO, including the four file uploads
                var dto = new DriverOnboardingDto
                {
                    FirstName = vm.FirstName,
                    LastName = vm.LastName,
                    Email = vm.Email,
                    PhoneNumber = vm.PhoneNumber,
                    Address = vm.Address,
                    DateOfBirth = vm.DateOfBirth,
                    Gender = vm.Gender,
                    EmploymentStatus = vm.EmploymentStatus,
                    LicenseNumber = vm.LicenseNumber,
                    LicenseExpiryDate = vm.LicenseExpiryDate,
                    CompanyBranchId = vm.CompanyBranchId,
                    LicenseCategory = vm.LicenseCategory,
                    ShiftStatus = vm.ShiftStatus,

                    // New file properties:
                    LicensePhoto = vm.LicensePhoto,
                    ProfilePhoto = vm.ProfilePhoto
                };

                var result = await _driverService.OnboardDriverAsync(dto, _authUser.UserId);
                if (!result.Success)
                {
                    ModelState.AddModelError("", result.Message);
                    return View(vm);
                }

                TempData["SuccessMessage"] = result.Message;
                return RedirectToAction(nameof(Index));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error onboarding driver");
                ModelState.AddModelError("", "An unexpected error occurred.");
                return View(vm);
            }
        }

        // ─── EDIT ────────────────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Edit(long id)
        {
            try
            {
                var dto = await _driverService.GetDriverByIdAsync(id);
                if (dto == null) return NotFound();

                var vm = new DriverEditViewModel
                {
                    Id = dto.Id,
                    FirstName = dto.FirstName,
                    LastName = dto.LastName,
                    Email = dto.Email,
                    PhoneNumber = dto.PhoneNumber,
                    Address = dto.Address,
                    DateOfBirth = dto.DateOfBirth,
                    Gender = dto.Gender,
                    EmploymentStatus = dto.EmploymentStatus,
                    LicenseNumber = dto.LicenseNumber,
                    LicenseExpiryDate = dto.LicenseExpiryDate,
                    CompanyBranchId = dto.CompanyBranchId,
                    LicenseCategory = dto.LicenseCategory,
                    ShiftStatus = dto.ShiftStatus,
                    IsActive = dto.IsActive,

                    // existing photos/documents (for display in the edit view)
                    ExistingLicensePhotos = dto.Documents,
                    ExistingProfilePhotos = dto.Photos
                };

                // repopulate dropdowns
                var branches = await _branchService.GetBranchesForCompanyAsync();
                vm.Branches = branches.Select(b => new SelectListItem(b.Name, b.Id.ToString(), b.Id == dto.CompanyBranchId));
                vm.Genders = EnumHelper.ToSelectList<Gender>();
                vm.EmploymentStatuses = EnumHelper.ToSelectList<EmploymentStatus>();
                vm.ShiftStatuses = EnumHelper.ToSelectList<ShiftStatus>();
                vm.LicenseCategories = EnumHelper.ToSelectList<LicenseCategory>();

                return View(vm);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading driver for edit Id {DriverId}", id);
                return View("Error");
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(DriverEditViewModel vm)
        {
            try
            {
                // repopulate dropdowns on POST
                var branches = await _branchService.GetBranchesForCompanyAsync();
                vm.Branches = branches.Select(b => new SelectListItem(b.Name, b.Id.ToString(), b.Id == vm.CompanyBranchId));
                vm.Genders = EnumHelper.ToSelectList<Gender>();
                vm.EmploymentStatuses = EnumHelper.ToSelectList<EmploymentStatus>();
                vm.ShiftStatuses = EnumHelper.ToSelectList<ShiftStatus>();
                vm.LicenseCategories = EnumHelper.ToSelectList<LicenseCategory>();

                if (!ModelState.IsValid)
                    return View(vm);

                // map back to DTO, including any *new* uploads
                var dto = new DriverDto
                {
                    Id = vm.Id,
                    FirstName = vm.FirstName,
                    LastName = vm.LastName,
                    Email = vm.Email,
                    PhoneNumber = vm.PhoneNumber,
                    Address = vm.Address,
                    DateOfBirth = vm.DateOfBirth,
                    Gender = vm.Gender,
                    EmploymentStatus = vm.EmploymentStatus,
                    LicenseNumber = vm.LicenseNumber,
                    LicenseExpiryDate = vm.LicenseExpiryDate,
                    CompanyBranchId = vm.CompanyBranchId,
                    LicenseCategory = vm.LicenseCategory,
                    ShiftStatus = vm.ShiftStatus,
                    IsActive = vm.IsActive,

                    // allow replacing or adding new files
                    NewLicensePhoto = vm.LicensePhoto,
                    NewProfilePhoto = vm.ProfilePhoto
                };

                var result = await _driverService.UpdateDriverAsync(dto, _authUser.UserId);
                if (!result.Success)
                {
                    ModelState.AddModelError("", result.Message);
                    return View(vm);
                }

                TempData["SuccessMessage"] = "Driver updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating driver Id {DriverId}", vm.Id);
                ModelState.AddModelError("", "An unexpected error occurred.");
                return View(vm);
            }
        }

        // ─── DELETE ──────────────────────────────────────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(long id)
        {
            try
            {
                var result = await _driverService.DeleteDriverAsync(id);
                if (!result.Success)
                    TempData["ErrorMessage"] = result.Message;
                else
                    TempData["SuccessMessage"] = "Driver deleted successfully.";

                return RedirectToAction(nameof(Index));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting driver Id {DriverId}", id);
                TempData["ErrorMessage"] = "An unexpected error occurred.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}