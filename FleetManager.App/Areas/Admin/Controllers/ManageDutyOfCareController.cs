using FleetManager.Business.DataObjects;
using FleetManager.Business.DataObjects.VehicleDto;
using FleetManager.Business.Enums;
using FleetManager.Business.Interfaces.DutyOfCareModule;
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
    public class ManageDutyOfCareController : Controller
    {
        private readonly IDriverDutyOfCareService _dutyService;
        private readonly IManageDriverService _driverService;
        private readonly IAdminVehicleService _vehicleService;
        private readonly IAuthUser _authUser;
        private readonly ILogger<ManageDutyOfCareController> _logger;

        public ManageDutyOfCareController(
            IDriverDutyOfCareService dutyService,
            IManageDriverService driverService,
            IAdminVehicleService vehicleService,
            IAuthUser authUser,
            ILogger<ManageDutyOfCareController> logger)
        {
            _dutyService = dutyService;
            _driverService = driverService;
            _vehicleService = vehicleService;
            _authUser = authUser;
            _logger = logger;
        }

        // ─── INDEX: Show *all* Duty‑of‑Care records ───────────────────────────────────
        public async Task<IActionResult> Index()
        {
            try
            {
                // Build IQueryable<DriverDutyOfCareDto>, then ToListAsync
                var query = _dutyService
                    .QueryAll()
                    .OrderByDescending(d => d.Date);

                var list = await query.ToListAsync();
                return View(list);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading duty‑of‑care list");
                return View("Error");
            }
        }

        // ─── GET: Show form to create a new Duty‑of‑Care record ──────────────────────
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            try
            {                

                // 1) Load all drivers *in my branch*
                var drivers = await _driverService.GetDriversForBranchAsync();
                ViewBag.DriverSelect = drivers
                    .Select(d => new SelectListItem(d.FullName, d.Id.ToString()))
                    .ToList();

                // 2) Load all vehicles *in my branch*
                var vehicles = await _vehicleService.GetVehiclesAsync(
                    new VehicleFilterDto { BranchId = _authUser.CompanyBranchId });

                ViewBag.VehicleSelect = vehicles
                    .Select(v => new SelectListItem(
                        $"{v.Make} {v.Model} ({v.PlateNo})",
                        v.Id.ToString()))
                    .ToList();

                var recordType = _dutyService.GetDutyOfCareTypeOptions();
                ViewBag.DutyOfCareTypeSelect = recordType
                    .Select(d => new SelectListItem(d.Text, d.Value.ToString()))
                    .ToList();

                var statusType = _dutyService.GetDutyOfCareStatusOptions();
                ViewBag.DutyOfCareStatusSelect = statusType
                    .Select(d => new SelectListItem(d.Text, d.Value.ToString()))
                    .ToList();

                // 3) Prepare a blank DTO for the form
                var dto = new DutyOfCareViewModel
                {
                    Input = new DutyOfCareInputModel
                    {
                        Date = DateTime.Today,
                        DeclarationTimestamp = DateTime.UtcNow,
                        DutyOfCareRecordType = DutyOfCareRecordType.HealthCheck,
                        DutyOfCareStatus = DriverDutyOfCareStatus.PendingReview
                    },
                    DutyOfCareStatus = statusType,
                    DutyOfCareType = recordType
                };

                return View(dto);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading create duty‑of‑care form");
                return View("Error");
            }
        }

        // ─── POST: Create a new Duty‑of‑Care record ──────────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DutyOfCareViewModel vm)
        {
            try
            {
                // repopulate dropdowns on POST
                var drivers = await _driverService.GetDriversForBranchAsync();
                ViewBag.DriverSelect = drivers
                    .Select(d => new SelectListItem(d.FullName, d.Id.ToString(), d.Id == vm.Input.DriverId))
                    .ToList();

                var vehicles = await _vehicleService.GetVehiclesAsync(
                    new VehicleFilterDto { BranchId = _authUser.CompanyBranchId });

                ViewBag.VehicleSelect = vehicles
                    .Select(v => new SelectListItem(
                        $"{v.Make} {v.Model} ({v.PlateNo})",
                        v.Id.ToString(),
                        v.Id == vm.Input.VehicleId))
                    .ToList();

                var recordType = _dutyService.GetDutyOfCareTypeOptions();
                ViewBag.DutyOfCareTypeSelect = recordType
                    .Select(d => new SelectListItem(d.Text, d.Value.ToString()))
                    .ToList();

                var statusType = _dutyService.GetDutyOfCareStatusOptions();
                ViewBag.DutyOfCareStatusSelect = statusType
                    .Select(d => new SelectListItem(d.Text, d.Value.ToString()))
                    .ToList();

                if (!ModelState.IsValid)
                    return View(vm);
                // map ViewModel → DTO
                var dto = new DriverDutyOfCareDto
                {
                    // basic fields
                    DriverId = vm.Input.DriverId,
                    VehicleId = vm.Input.VehicleId,
                    Date = vm.Input.Date,

                    // Vehicle Responsibility
                    VehiclePreCheckCompleted = vm.Input.VehiclePreCheckCompleted,
                    VehicleConditionNotes = vm.Input.VehicleConditionNotes,

                    // Health & Fitness
                    IsFitToDrive = vm.Input.IsFitToDrive,
                    HealthDeclarationNotes = vm.Input.HealthDeclarationNotes,

                    // Legal Compliance
                    HasValidLicense = vm.Input.HasValidLicense,
                    IsAwareOfCompanyPolicies = vm.Input.IsAwareOfCompanyPolicies,
                    HasReviewedDrivingHours = vm.Input.HasReviewedDrivingHours,

                    // Fatigue Management
                    LastRestPeriod = vm.Input.LastRestPeriod,
                    ReportsFatigue = vm.Input.ReportsFatigue,

                    // Incident / Hazard Awareness
                    ReportsVehicleIssues = vm.Input.ReportsVehicleIssues,
                    ReportedIssuesDetails = vm.Input.ReportedIssuesDetails,

                    // Consent & Declaration
                    ConfirmsAccuracyOfInfo = vm.Input.ConfirmsAccuracyOfInfo,
                    DeclarationTimestamp = vm.Input.DeclarationTimestamp,

                    // Enums
                    DutyOfCareRecordType = vm.Input.DutyOfCareRecordType,
                    DutyOfCareStatus = vm.Input.DutyOfCareStatus
                };

                var result = await _dutyService.CreateAsync(dto, _authUser.UserId);
                if (!result.Success)
                {
                    ModelState.AddModelError(string.Empty, result.Message);
                    return View(vm);
                }


                TempData["Success"] = "Duty‑of‑care record created successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating duty‑of‑care record");
                ModelState.AddModelError("", "An unexpected error occurred.");
                return View(vm);
            }
        }


        public async Task<IActionResult> Details(long id)
        {
            var duty = await _dutyService.GetByIdAsync(id);
            if (duty == null) return NotFound();

            return View(duty);
        }


        // ─── GET: Edit an existing Duty‑of‑Care record ───────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Edit(long id)
        {
            try
            {
                var dto = await _dutyService.GetByIdAsync(id);
                if (dto == null) return NotFound();

                // repopulate dropdowns
                var drivers = await _driverService.GetDriversForBranchAsync();
                ViewBag.DriverSelect = drivers
                    .Select(d => new SelectListItem(d.FullName, d.Id.ToString(), d.Id == dto.DriverId))
                    .ToList();

                var vehicles = await _vehicleService.GetVehiclesAsync(
                    new VehicleFilterDto { BranchId = _authUser.CompanyBranchId });

                ViewBag.VehicleSelect = vehicles
                    .Select(v => new SelectListItem(
                        $"{v.Make} {v.Model} ({v.PlateNo})",
                        v.Id.ToString(),
                        v.Id == dto.VehicleId))
                    .ToList();

                return View(dto);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading edit form for duty‑of‑care Id {Id}", id);
                return View("Error");
            }
        }

        // ─── POST: Save updates to a Duty‑of‑Care record ─────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(DriverDutyOfCareDto dto)
        {
            try
            {
                // repopulate dropdowns on POST
                var drivers = await _driverService.GetDriversForBranchAsync();
                ViewBag.DriverSelect = drivers
                    .Select(d => new SelectListItem(d.FullName, d.Id.ToString(), d.Id == dto.DriverId))
                    .ToList();

                var vehicles = await _vehicleService.GetVehiclesAsync(
                    new VehicleFilterDto { BranchId = _authUser.CompanyBranchId });

                ViewBag.VehicleSelect = vehicles
                    .Select(v => new SelectListItem(
                        $"{v.Make} {v.Model} ({v.PlateNo})",
                        v.Id.ToString(),
                        v.Id == dto.VehicleId))
                    .ToList();

                if (!ModelState.IsValid)
                    return View(dto);

                var result = await _dutyService.UpdateAsync(dto, _authUser.UserId);
                if (!result.Success)
                {
                    ModelState.AddModelError("", result.Message);
                    return View(dto);
                }

                TempData["Success"] = "Duty‑of‑care record updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating duty‑of‑care Id {Id}", dto.Id);
                ModelState.AddModelError("", "An unexpected error occurred.");
                return View(dto);
            }
        }

        // ─── POST: Delete (Unassign) a Duty‑of‑Care record ───────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(long id)
        {
            try
            {
                var result = await _dutyService.DeleteAsync(id);
                if (!result.Success)
                    TempData["Error"] = result.Message;
                else
                    TempData["Success"] = "Duty‑of‑care record deleted successfully.";

                return RedirectToAction(nameof(Index));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting duty‑of‑care Id {Id}", id);
                TempData["Error"] = "An unexpected error occurred.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
