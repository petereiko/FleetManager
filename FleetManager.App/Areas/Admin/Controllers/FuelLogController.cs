using FleetManager.Business.DataObjects;
using FleetManager.Business.DataObjects.VehicleDto;
using FleetManager.Business.Interfaces.ComapyBranchModule;
using FleetManager.Business.Interfaces.FuelLogModule;
using FleetManager.Business.Interfaces.ManageDriverModule;
using FleetManager.Business.Interfaces.UserModule;
using FleetManager.Business.Interfaces.VehicleModule;
using FleetManager.Business.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FleetManager.App.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class FuelLogController : Controller
    {
        private readonly IFuelLogService _fuelLogService;
        private readonly IAdminVehicleService _vehicleService;
        private readonly IAuthUser _authUser;
        private readonly ILogger<FuelLogController> _logger;
        private readonly IManageDriverService _driverService;

        public FuelLogController(
            IFuelLogService fuelLogService,
            IAuthUser authUser,
            ILogger<FuelLogController> logger,
            IManageDriverService driverService,
            IAdminVehicleService vehicleService)
        {
            _fuelLogService = fuelLogService;
            _authUser = authUser;
            _logger = logger;
            _driverService = driverService;
            _vehicleService = vehicleService;
        }

        // ─── INDEX / LIST ────────────────────────────────────────────────────────────
        public async Task<IActionResult> Index()
        {
            try
            {
                // 1) determine if owner/super (global) or branch‐admin
                var roles = (_authUser.Roles ?? "")
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(r => r.Trim());
                bool isGlobal = roles.Contains("Company Owner") || roles.Contains("Super Admin");

                // 2) get the raw DTO query
                var query = _fuelLogService.QueryByBranch(isGlobal ? null : _authUser.CompanyBranchId);

                // 3) fetch and project into your list‐item VM
                var list = await query
                    .OrderByDescending(f => f.Date)
                    .Select(d => new FuelLogListItemViewModel
                    {
                        Id = d.Id,
                        Date = d.Date,
                        DriverName = d.DriverName,
                        VehicleDescription = d.VehicleDescription,
                        PlateNo=d.LicenseNo,
                        Odometer = d.Odometer,
                        Volume = d.Volume,
                        Cost = d.Cost,
                        FuelType = d.FuelType.ToString()
                    })
                    .ToListAsync();

                // 4) hand the correct VM type to the view
                return View(list);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading fuel logs");
                return View("Error");
            }
        }
        // ─── DETAILS ────────────────────────────────────────────────────────────
        public async Task<IActionResult> Details(long id)
        {
            try
            {
                var dto = await _fuelLogService.GetByIdAsync(id);
                if (dto == null) return NotFound();
                return View(dto);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching fuel log {FuelLogId}", id);
                return View("Error");
            }
        }

        // ─── CREATE ──────────────────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            try
            {
                var vm = new FuelLogInputViewModel
                {
                    Date = DateTime.Today,
                    FuelTypes = _fuelLogService.GetFuelTypeOptions()
                };

                // Drivers dropdown
                var drivers = await _driverService.GetDriversForBranchAsync(_authUser.CompanyBranchId);
                vm.Drivers = drivers.Select(d => new SelectListItem(d.FullName, d.Id.ToString())).ToList();

                // Vehicles dropdown
                var vehicles = await _vehicleService.GetVehiclesAsync(new VehicleFilterDto { BranchId = _authUser.CompanyBranchId });
                vm.Vehicles = vehicles.Select(v => new SelectListItem($"{v.Make} {v.Model}", v.Id.ToString())).ToList();

                return View(vm);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FuelLogInputViewModel vm)
        {
            try
            {
                // repopulate dropdowns
                vm.FuelTypes = _fuelLogService.GetFuelTypeOptions();
                var drivers = await _driverService.GetDriversForBranchAsync(_authUser.CompanyBranchId);
                vm.Drivers = drivers.Select(d => new SelectListItem(d.FullName, d.Id.ToString(), d.Id == vm.DriverId)).ToList();
                var vehicles = await _vehicleService.GetVehiclesAsync(new VehicleFilterDto { BranchId = _authUser.CompanyBranchId });
                vm.Vehicles = vehicles.Select(v => new SelectListItem($"{v.Make} {v.Model}", v.Id.ToString(), v.Id == vm.VehicleId)).ToList();

                if (!ModelState.IsValid)
                    return View(vm);

                var dto = new FuelLogInputDto
                {
                    DriverId = vm.DriverId,
                    VehicleId = vm.VehicleId,
                    Date = vm.Date,
                    Odometer = vm.Odometer,
                    Volume = vm.Volume,
                    Cost = vm.Cost,
                    FuelType = vm.FuelType,
                    ReceiptFile = vm.ReceiptFile,
                    Notes = vm.Notes
                };

                var result = await _fuelLogService.CreateAsync(dto, _authUser.UserId);
                if (!result.Success)
                {
                    ModelState.AddModelError("", result.Message);
                    return View(vm);
                }

                TempData["SuccessMessage"] = "Fuel log created successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating fuel log");
                ModelState.AddModelError("", "An unexpected error occurred.");
                return View(vm);
            }
        }

        // ─── EDIT ────────────────────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Edit(long id)
        {
            try
            {
                var dto = await _fuelLogService.GetByIdAsync(id);
                if (dto == null) return NotFound();

                var vm = new FuelLogInputViewModel
                {
                    Id = dto.Id,
                    DriverId = dto.DriverId,
                    VehicleId = dto.VehicleId,
                    Date = dto.Date,
                    Odometer = dto.Odometer,
                    Volume = dto.Volume,
                    Cost = dto.Cost,
                    FuelType = dto.FuelType,
                    Notes = dto.Notes,
                    ExistingReceiptPath = dto.ReceiptPath,
                    FuelTypes = _fuelLogService.GetFuelTypeOptions()
                };

                var drivers = await _driverService.GetDriversForBranchAsync(_authUser.CompanyBranchId);
                vm.Drivers = drivers.Select(d => new SelectListItem(d.FullName, d.Id.ToString(), d.Id == vm.DriverId)).ToList();
                var vehicles = await _vehicleService.GetVehiclesAsync(new VehicleFilterDto { BranchId = _authUser.CompanyBranchId });
                vm.Vehicles = vehicles.Select(v => new SelectListItem($"{v.Make} {v.Model}", v.Id.ToString(), v.Id == vm.VehicleId)).ToList();

                return View(vm);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading fuel log {FuelLogId}", id);
                return View("Error");
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(FuelLogInputViewModel vm)
        {
            try
            {
                vm.FuelTypes = _fuelLogService.GetFuelTypeOptions();
                var drivers = await _driverService.GetDriversForBranchAsync(_authUser.CompanyBranchId);
                vm.Drivers = drivers.Select(d => new SelectListItem(d.FullName, d.Id.ToString(), d.Id == vm.DriverId)).ToList();
                var vehicles = await _vehicleService.GetVehiclesAsync(new VehicleFilterDto { BranchId = _authUser.CompanyBranchId });
                vm.Vehicles = vehicles.Select(v => new SelectListItem($"{v.Make} {v.Model}", v.Id.ToString(), v.Id == vm.VehicleId)).ToList();

                if (!ModelState.IsValid)
                    return View(vm);

                var dto = new FuelLogInputDto
                {
                    DriverId = vm.DriverId,
                    VehicleId = vm.VehicleId,
                    Date = vm.Date,
                    Odometer = vm.Odometer,
                    Volume = vm.Volume,
                    Cost = vm.Cost,
                    FuelType = vm.FuelType,
                    ReceiptFile = vm.ReceiptFile,
                    Notes = vm.Notes
                };

                var result = await _fuelLogService.UpdateAsync(vm.Id!.Value, dto, _authUser.UserId);
                if (!result.Success)
                {
                    ModelState.AddModelError("", result.Message);
                    return View(vm);
                }

                TempData["SuccessMessage"] = "Fuel log updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating fuel log {FuelLogId}", vm.Id);
                ModelState.AddModelError("", "An unexpected error occurred.");
                return View(vm);
            }
        }

        // ─── DELETE ──────────────────────────────────────────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(long id)
        {
            try
            {
                var result = await _fuelLogService.DeleteAsync(id);
                if (!result.Success)
                    TempData["ErrorMessage"] = result.Message;
                else
                    TempData["SuccessMessage"] = "Fuel log deleted successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting fuel log {FuelLogId}", id);
                TempData["ErrorMessage"] = "An unexpected error occurred.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
