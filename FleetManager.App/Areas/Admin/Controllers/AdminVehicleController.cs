using FleetManager.Business.DataObjects;
using FleetManager.Business.DataObjects.VehicleDto;
using FleetManager.Business.Enums;
using FleetManager.Business.Implementations.VehicleModule;
using FleetManager.Business.Interfaces.ComapyBranchModule;
using FleetManager.Business.Interfaces.CompanyModule;
using FleetManager.Business.Interfaces.UserModule;
using FleetManager.Business.Interfaces.VehicleModule;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FleetManager.App.Areas.Admin.Controllers
{
    [Area("Admin")]
    //[Authorize(Roles = "Company")]
    public class AdminVehicleController : Controller
    {
        private readonly IAdminVehicleService _vehicleService;
        private readonly IBranchService _branchService;
        private readonly IAuthUser _authUser;
        private readonly ILogger<AdminVehicleController> _logger;

        public AdminVehicleController(
            IAdminVehicleService vehicleService,
            IBranchService branchService,
            IAuthUser authUser,
            ILogger<AdminVehicleController> logger)
        {
            _vehicleService = vehicleService;
            _branchService = branchService;
            _authUser = authUser;
            _logger = logger;
        }

        // ─── INDEX / LIST ────────────────────────────────────────────────────────────
        public async Task<IActionResult> Index(VehicleFilterDto filter, int page = 1, int pageSize = 9)
        {
            try
            {
                // Determine roles
                var roles = (_authUser.Roles ?? "")
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(r => r.Trim());
                bool isGlobal = roles.Contains("Company Owner") || roles.Contains("Super Admin");

                // Admins only see their branch
                if (!isGlobal)
                    filter.BranchId = _authUser.CompanyBranchId;

                // Query & pagination
                var query = _vehicleService.QueryVehicles(filter);
                var total = await query.CountAsync();
                var vehicles = await query
                    .OrderBy(v => v.Make)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                // Build VM
                var vm = new VehicleIndexViewModel
                {
                    Filter = filter,
                    Vehicles = vehicles,
                    Pagination = new PaginationDto
                    {
                        CurrentPage = page,
                        PageSize = pageSize,
                        TotalItems = total
                    }
                };

                // Global roles get branch filter dropdown
                if (isGlobal)
                {
                    var branches = await _branchService.GetBranchesForCompanyAsync();
                    ViewBag.Branches = new SelectList(branches, "Id", "Name", filter.BranchId);
                }

                ViewBag.Statuses = new SelectList(
                    Enum.GetValues<VehicleStatus>()
                        .Cast<VehicleStatus>()
                        .Select(s => new { Id = (int)s, Name = s.ToString() }),
                    "Id", "Name", filter.Status);
                ViewBag.VehicleTypes = new SelectList(
                    Enum.GetValues<VehicleType>()
                        .Cast<VehicleType>()
                        .Select(t => new { Id = (int)t, Name = t.ToString() }),
                    "Id", "Name", filter.Type);

                return View(vm);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading vehicles");
                return View("Error");
            }
        }

        // ─── DETAILS ─────────────────────────────────────────────────────────────────
        public async Task<IActionResult> Details(long id)
        {
            try
            {
                var dto = await _vehicleService.GetVehicleByIdAsync(id);
                if (dto == null) return NotFound();
                return View(dto);
            }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching vehicle details");
                return View("Error");
            }
        }

        // ─── CREATE ──────────────────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            try
            {
                var vm = new VehicleEditViewModel();
                await PopulateSelectsAsync(vm);
                return View(vm);
            }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error preparing create vehicle");
                return View("Error");
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(VehicleDto input)
        {
            try
            {
                // reload selects if redisplay
                var vm = new VehicleEditViewModel(input);
                await PopulateSelectsAsync(vm);

                if (!ModelState.IsValid)
                    return View(vm);

                // map to DTO
                var dto = input.ToDto();
                dto.PhotoFiles = input.PhotoFiles;
                dto.DocumentFiles = input.DocumentFiles;

                var result = await _vehicleService.CreateVehicleAsync(dto, _authUser.UserId);
                if (!result.Success)
                {
                    ModelState.AddModelError("", result.Message);
                    return View(vm);
                }

                TempData["SuccessMessage"] = "Vehicle created successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating vehicle");
                ModelState.AddModelError("", "An unexpected error occurred.");
                var vm = new VehicleEditViewModel(input);
                await PopulateSelectsAsync(vm);
                return View(vm);
            }
        }

        // ─── EDIT ────────────────────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Edit(long id)
        {
            try
            {
                var dto = await _vehicleService.GetVehicleByIdAsync(id);
                if (dto == null) return NotFound();

                var vm = new VehicleEditViewModel(dto);
                await PopulateSelectsAsync(vm);
                return View(vm);
            }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading vehicle for edit");
                return View("Error");
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(VehicleDto input)
        {
            try
            {
                var vm = new VehicleEditViewModel(input)
                {
                    ExistingImages = input.ExistingImages,
                    ExistingDocuments = input.ExistingDocuments
                };
                await PopulateSelectsAsync(vm);

                if (!ModelState.IsValid)
                    return View(vm);

                var dto = input.ToDto();
                dto.PhotoFiles = input.PhotoFiles;
                dto.DocumentFiles = input.DocumentFiles;

                var result = await _vehicleService.UpdateVehicleAsync(dto, _authUser.UserId);
                if (!result.Success)
                {
                    ModelState.AddModelError("", result.Message);
                    return View(vm);
                }

                TempData["SuccessMessage"] = "Vehicle updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating vehicle");
                ModelState.AddModelError("", "An unexpected error occurred.");
                var vm = new VehicleEditViewModel(input);
                await PopulateSelectsAsync(vm);
                return View(vm);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Admin/Vehicle/UpdateVehicleStatus")]
        public async Task<IActionResult> UpdateVehicleStatus(long vehicleId, VehicleStatus newStatus)
        {
            var success = await _vehicleService.UpdateVehicleStatusAsync(vehicleId, newStatus);
            if (!success)
            {
                return BadRequest("Failed to update vehicle status.");
            }

            return Ok(new { message = "Vehicle status updated successfully." });
        }


        // ─── DELETE ──────────────────────────────────────────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(long id)
        {
            try
            {
                var result = await _vehicleService.DeleteVehicleAsync(id);
                if (!result.Success) TempData["ErrorMessage"] = result.Message;
                else TempData["SuccessMessage"] = "Vehicle deleted successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting vehicle");
                TempData["ErrorMessage"] = "An unexpected error occurred.";
                return RedirectToAction(nameof(Index));
            }
        }

        // ─── HELPERS ─────────────────────────────────────────────────────────────────
        private async Task PopulateSelectsAsync(VehicleEditViewModel vm)
        {
            // determine global vs. branch-only
            var roles = (_authUser.Roles ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(r => r.Trim());
            bool isGlobal = roles.Contains("Company Owner") || roles.Contains("Super Admin");

            if (isGlobal)
            {
                vm.Branches = await _vehicleService.GetBranchOptionsAsync(_authUser.CompanyId.Value);
            }
            else
            {
                var all = await _branchService.GetBranchesForCompanyAsync();
                var me = all.First(b => b.Id == _authUser.CompanyBranchId);
                vm.Branches = new[]
                {
                    new SelectListItem { Value = me.Id.ToString(), Text = me.Name }
                };
                vm.CompanyBranchId = _authUser.CompanyBranchId.Value;
            }

            vm.FuelTypes = _vehicleService.GetFuelTypeOptions();
            vm.TransmissionTypes = _vehicleService.GetTransmissionTypeOptions();
            vm.Statuses = _vehicleService.GetStatusOptions();
            vm.VehicleTypes = _vehicleService.GetVehicleTypeOptions();
        }
    }

    // Implicit cast helper (or you can use AutoMapper)
    // Map input → service DTO
    public static class VehicleMappingExtensions
    {
        public static VehicleDto ToDto(this VehicleDto m) => new VehicleDto
        {
            Id = m.Id,
            Make = m.Make,
            Model = m.Model,
            Year = m.Year,
            VIN = m.VIN,
            PlateNo = m.PlateNo,
            Color = m.Color,
            EngineNumber = m.EngineNumber,
            ChassisNumber = m.ChassisNumber,
            RegistrationDate = m.RegistrationDate,
            LastServiceDate = m.LastServiceDate,
            Mileage = m.Mileage,
            FuelType = m.FuelType,
            TransmissionType = m.TransmissionType,
            InsuranceCompany = m.InsuranceCompany,
            InsuranceExpiryDate = m.InsuranceExpiryDate,
            RoadWorthyExpiryDate = m.RoadWorthyExpiryDate,
            CompanyBranchId = m.CompanyBranchId,
            VehicleStatus = m.VehicleStatus,
            VehicleType = m.VehicleType
        };
    }
}

