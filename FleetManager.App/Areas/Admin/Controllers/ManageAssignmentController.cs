using FleetManager.Business.DataObjects;
using FleetManager.Business.DataObjects.VehicleDto;
using FleetManager.Business.Interfaces.DriverVehicleModule;
using FleetManager.Business.Interfaces.ManageDriverModule;
using FleetManager.Business.Interfaces.UserModule;
using FleetManager.Business.Interfaces.VehicleModule;
using FleetManager.Business.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FleetManager.App.Areas.Admin.Controllers
{
    [Authorize]
    [Area("Admin")]
    public class ManageAssignmentController : Controller
    {
        private readonly IDriverVehicleService _assignmentService;
        private readonly IManageDriverService _driverService;
        private readonly IAdminVehicleService _vehicleService;
        private readonly IAuthUser _authUser;
        private readonly ILogger<ManageAssignmentController> _logger;

        public ManageAssignmentController(
            IDriverVehicleService assignmentService,
            IManageDriverService driverService,
            IAdminVehicleService vehicleService,
            IAuthUser authUser,
            ILogger<ManageAssignmentController> logger)
        {
            _assignmentService = assignmentService;
            _driverService = driverService;
            _vehicleService = vehicleService;
            _authUser = authUser;
            _logger = logger;
        }

        // ─── INDEX: List assignments, optional filter by driver ─────────────────
        public async Task<IActionResult> Index(long? driverId)
        {
            try
            {


                var drivers = await _driverService
                    .GetDriversForBranchAsync()        // returns List<DriverListItemDto>
                    .ConfigureAwait(false);

                // build select list
                ViewBag.DriverFilter = drivers
                    .Select(d => new SelectListItem(d.FullName, d.Id.ToString(), d.Id == driverId))
                    .ToList();

                // query assignments
                var q = _assignmentService
                    .QueryAssignmentsByDriver(driverId ?? 0);

                // optionally if driverId is null show all:
                if (!driverId.HasValue)
                {
                    // flatten all drivers in branch
                    q = drivers
                        .SelectMany(d => _assignmentService.QueryAssignmentsByDriver(d.Id))
                        .AsQueryable();
                }

                var assignments = q
                    .OrderBy(a => a.StartDate)
                    .ToList();


                var vm = new AssignmentIndexViewModel
                {
                    DriverFilterId = driverId,
                    Assignments = assignments
                };

                return View(vm);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading assignments");
                return View("Error");
            }
        }

        // ─── GET: Show form to assign a vehicle 
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            try
            {
                var vm = new AssignmentCreateViewModel();

                var drivers = await _driverService.GetDriversForBranchAsync();
                var vehicles = await GetAvailableVehiclesAsync(); // ← only available vehicles

                vm.Drivers = drivers
                    .Select(d => new SelectListItem(d.FullName, d.Id.ToString()))
                    .ToList();

                vm.Vehicles = vehicles
                    .Select(v => new SelectListItem($"{v.Make} {v.Model} ({v.PlateNo})", v.Id.ToString()))
                    .ToList();

                vm.Input.StartDate = DateTime.Today;

                return View(vm);
            }
            catch (UnauthorizedAccessException) { return Forbid(); }
        }

        // ─── POST: Create new assignment 
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AssignmentCreateViewModel vm)
        {
            try
            {
                var drivers = await _driverService.GetDriversForBranchAsync();
                var vehicles = await GetAvailableVehiclesAsync();

                vm.Drivers = drivers
                    .Select(d => new SelectListItem(d.FullName, d.Id.ToString(), d.Id == vm.Input.DriverId))
                    .ToList();

                vm.Vehicles = vehicles
                    .Select(v => new SelectListItem($"{v.Make} {v.Model} ({v.PlateNo})", v.Id.ToString(), v.Id == vm.Input.VehicleId))
                    .ToList();

                if (!ModelState.IsValid)
                    return View(vm);

                // Guard: re-check the selected vehicle is still available at submit time
                // (another admin could have assigned it between page load and submit)
                var assignedIds = await _assignmentService.GetCurrentlyAssignedVehicleIdsAsync();
                if (assignedIds.Contains(vm.Input.VehicleId))
                {
                    ModelState.AddModelError("", "This vehicle has just been assigned to another driver. Please select a different vehicle.");
                    return View(vm);
                }

                var dto = new DriverVehicleDto
                {
                    DriverId = vm.Input.DriverId,
                    VehicleId = vm.Input.VehicleId,
                    StartDate = vm.Input.StartDate,
                    EndDate = vm.Input.EndDate
                };

                var result = await _assignmentService.AssignVehicleAsync(dto, _authUser.UserId);

                if (!result.Success)
                {
                    ModelState.AddModelError("", result.Message);
                    return View(vm);
                }

                TempData["Success"] = "Vehicle assigned successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning vehicle");
                ModelState.AddModelError("", "An unexpected error occurred.");
                return View(vm);
            }
        }

        // ─── GET: Edit an assignment 
        [HttpGet]
        public async Task<IActionResult> Edit(long id)
        {
            try
            {
                var dto = await _assignmentService
                    .QueryAssignmentsByDriver(0)
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (dto == null) return NotFound();

                var vm = new AssignmentEditViewModel
                {
                    Id = dto.Id,
                    Input = new AssignmentInputModel
                    {
                        DriverId = dto.DriverId,
                        VehicleId = dto.VehicleId,
                        StartDate = dto.StartDate,
                        EndDate = dto.EndDate
                    }
                };

                var drivers = await _driverService.GetDriversForBranchAsync();
                // Exclude this assignment's vehicle from "assigned" list so it stays selectable
                var vehicles = await GetAvailableVehiclesAsync(excludeAssignmentId: id);

                vm.Drivers = drivers
                    .Select(d => new SelectListItem(d.FullName, d.Id.ToString(), d.Id == vm.Input.DriverId))
                    .ToList();

                vm.Vehicles = vehicles
                    .Select(v => new SelectListItem($"{v.Make} {v.Model} ({v.PlateNo})", v.Id.ToString(), v.Id == vm.Input.VehicleId))
                    .ToList();

                return View(vm);
            }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading assignment Id {AssignmentId}", id);
                return View("Error");
            }
        }

        // ─── POST: Update assignment 
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(AssignmentEditViewModel vm)
        {
            try
            {
                var drivers = await _driverService.GetDriversForBranchAsync();
                var vehicles = await GetAvailableVehiclesAsync(excludeAssignmentId: vm.Id);

                vm.Drivers = drivers
                    .Select(d => new SelectListItem(d.FullName, d.Id.ToString(), d.Id == vm.Input.DriverId))
                    .ToList();

                vm.Vehicles = vehicles
                    .Select(v => new SelectListItem($"{v.Make} {v.Model} ({v.PlateNo})", v.Id.ToString(), v.Id == vm.Input.VehicleId))
                    .ToList();

                if (!ModelState.IsValid)
                    return View(vm);

                var dto = new DriverVehicleDto
                {
                    Id = vm.Id,
                    DriverId = vm.Input.DriverId,
                    VehicleId = vm.Input.VehicleId,
                    StartDate = vm.Input.StartDate,
                    EndDate = vm.Input.EndDate
                };

                var result = await _assignmentService.UpdateAssignmentAsync(dto, _authUser.UserId);

                if (!result.Success)
                {
                    ModelState.AddModelError("", result.Message);
                    return View(vm);
                }

                TempData["Success"] = "Assignment updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating assignment Id {AssignmentId}", vm.Id);
                ModelState.AddModelError("", "An unexpected error occurred.");
                return View(vm);
            }
        }

        // ─── POST: Unassign (delete) 
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(long id)
        {
            try
            {
                var result = await _assignmentService.UnassignVehicleAsync(id);
                if (!result.Success)
                    TempData["Error"] = result.Message;
                else
                    TempData["Success"] = "Vehicle unassigned.";

                return RedirectToAction(nameof(Index));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting assignment Id {AssignmentId}", id);
                TempData["Error"] = "An unexpected error occurred.";
                return RedirectToAction(nameof(Index));
            }
        }

        private async Task<List<VehicleListItemDto>> GetAvailableVehiclesAsync(long? excludeAssignmentId = null)
        {
            var allVehicles = await _vehicleService.GetVehiclesAsync(new VehicleFilterDto
            {
                BranchId = _authUser.CompanyBranchId
            });

            var assignedIds = await _assignmentService.GetCurrentlyAssignedVehicleIdsAsync(excludeAssignmentId);

            return allVehicles
                .Where(v => !assignedIds.Contains(v.Id))
                .ToList();
        }

    }
}
