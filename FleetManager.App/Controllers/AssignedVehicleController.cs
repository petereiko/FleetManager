using FleetManager.App.Areas.Admin.Controllers;
using FleetManager.Business.Database.Entities;
using FleetManager.Business.DataObjects.RepairDto;
using FleetManager.Business.DataObjects.VehicleDto;
using FleetManager.Business.Interfaces.DriverVehicleModule;
using FleetManager.Business.Interfaces.RepairModule;
using FleetManager.Business.Interfaces.UserModule;
using FleetManager.Business.Interfaces.VehicleModule;
using FleetManager.Business.ViewModels;
using FleetManager.Business.ViewModels.RepairHistoryViewModels;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Threading.Tasks;

namespace FleetManager.App.Controllers
{
    public class AssignedVehicleController : Controller
    {
        
        private readonly IAuthUser _authUser;
        private readonly IDriverVehicleService _assignmentService;
        private readonly IAdminVehicleService _vehicleService;
        private readonly ILogger<AssignedVehicleController> _logger;
        private readonly IRepairService _service;
        private readonly IDataProtector _protector;

        public AssignedVehicleController(IAuthUser authUser, IDriverVehicleService assignmentService, IAdminVehicleService vehicleService, IDataProtectionProvider dataProtectionProvider, IRepairService service)
        {
            _authUser = authUser;
            _assignmentService = assignmentService;
            _vehicleService = vehicleService;
            _protector = dataProtectionProvider
            .CreateProtector("VehicleIdProtector");
            _service = service;
        }

        //public async Task<IActionResult> Index()
        //{
        //    try
        //    {
        //        var userId = _authUser.UserId;
        //        var driverId = await _assignmentService.GetDriverIdByUserAsync(userId);

        //        // query assignments
        //        var q = _assignmentService
        //            .QueryAssignmentsByDriver(driverId);



        //        var assignments = q
        //            .OrderBy(a => a.StartDate)
        //            .ToList();


        //        var vm = new AssignmentIndexViewModel
        //        {
        //            DriverFilterId = driverId,
        //            Assignments = assignments
        //        };

        //        return View(vm);
        //    }
        //    catch (UnauthorizedAccessException)
        //    {
        //        return Forbid();
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error loading assignments");
        //        return View("Error");
        //    }
        //}


        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var userId = _authUser.UserId;
                var driverId = await _assignmentService.GetDriverIdByUserAsync(userId);

                var dtoList = _assignmentService
                    .QueryAssignmentsByDriver(driverId)
                    .OrderBy(a => a.StartDate)
                    .ToList();

                var vm = new AssignmentIndexViewModel
                {
                    DriverFilterId = driverId,
                    AssignedVehicles = dtoList.Select(a => new AssignedVehicleViewModel
                    {
                        VehicleMakeModel = a.VehicleMakeModel,
                        PlateNo = a.PlateNo,
                        StartDate = a.StartDate,
                        EndDate = a.EndDate,
                        EncodedVehicleId = _protector.Protect(a.VehicleId.ToString())
                    }).ToList()
                };

                return View(vm);
            }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading assignments");
                return View("Error");
            }
        }



         //─── DETAILS ─────────────────────────────────────────────────────────────────
        //public async Task<IActionResult> Details(long id)
        //{
        //    try
        //    {
        //        var dto = await _vehicleService.GetVehicleByIdAsync(id);
        //        if (dto == null) return NotFound();
        //        return View(dto);
        //    }
        //    catch (UnauthorizedAccessException) { return Forbid(); }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error fetching vehicle details");
        //        return View("Error");
        //    }
        //}


        //[HttpGet]
        //public async Task<IActionResult> Details(string protectedId)
        //{
        //    if (string.IsNullOrWhiteSpace(protectedId))
        //        return BadRequest("Missing vehicle identifier.");

        //    long vehicleId;
        //    try
        //    {
        //        var unwrapped = _protector.Unprotect(protectedId);
        //        vehicleId = long.Parse(unwrapped);
        //    }
        //    catch
        //    {
        //        return BadRequest("Invalid vehicle identifier.");
        //    }

        //    var dto = await _vehicleService.GetVehicleByIdAsync(vehicleId);
        //    if (dto == null) return NotFound();

        //    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        //        return PartialView("_VehicleDetailsPartial", dto);

        //    return View(dto);
        //}

        [HttpGet]
        public async Task<IActionResult> Details(string protectedId)
        {
            if (string.IsNullOrWhiteSpace(protectedId))
                return BadRequest("Missing vehicle identifier.");

            long vehicleId;
            try
            {
                var unwrapped = _protector.Unprotect(protectedId);
                vehicleId = long.Parse(unwrapped);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Invalid vehicle identifier");
                return View("Error");
            }

            var dto = await _vehicleService.GetVehicleByIdAsync(vehicleId);
            if (dto == null) return NotFound();

            return View(dto);
        }



        [HttpGet]
        public async Task<IActionResult> VehicleRepairHistoryPartial(long vehicleId, int page = 1, int pageSize = 10)
        {
            try
            {
                // call the service
                var resp = await _service.QueryRepairsByVehicleAsync(page, pageSize, vehicleId);
                if (!resp.Success)
                {
                    // return a simple partial that shows error (so client can display it inside the modal)
                    return PartialView("_VehicleRepairHistoryPartial", new VehicleRepairHistoryPartialViewModel
                    {
                        VehicleId = vehicleId,
                        VehicleDescription = $"Vehicle #{vehicleId}",
                        Repairs = Array.Empty<RepairDto>(),
                        Pagination = new PaginationDto { CurrentPage = page, PageSize = pageSize, TotalItems = 0 },
                        ErrorMessage = resp.Message
                    });
                }

                var paged = resp.Result;

                // optionally try to get nicer vehicle description (best-effort)
                string vehicleTitle = paged.Items.FirstOrDefault()?.VehicleDescription ?? $"Vehicle #{vehicleId}";
                try
                {
                    var veh = await _vehicleService.GetVehicleByIdAsync(vehicleId);
                    if (veh != null)
                    {
                        var make = veh.Make ?? string.Empty;
                        var model = veh.Model ?? string.Empty;
                        var plate = veh.PlateNo ?? string.Empty;
                        vehicleTitle = $"{plate} — {make} {model}".Trim();
                    }
                }
                catch
                {
                    // ignore; not critical
                }

                var vm = new VehicleRepairHistoryPartialViewModel
                {
                    VehicleId = vehicleId,
                    VehicleDescription = vehicleTitle,
                    Repairs = paged.Items,
                    Pagination = new PaginationDto
                    {
                        CurrentPage = paged.Page,
                        PageSize = paged.PageSize,
                        TotalItems = paged.TotalPages // adapt if your PaginatedResult exposes TotalItems
                    }
                };

                return PartialView("_VehicleRepairHistoryPartial", vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading vehicle repair history partial for {VehicleId}", vehicleId);
                var vm = new VehicleRepairHistoryPartialViewModel
                {
                    VehicleId = vehicleId,
                    VehicleDescription = $"Vehicle #{vehicleId}",
                    Repairs = Array.Empty<RepairDto>(),
                    Pagination = new PaginationDto { CurrentPage = page, PageSize = pageSize, TotalItems = 0 },
                    ErrorMessage = "Failed to load repair history."
                };
                return PartialView("_VehicleRepairHistoryPartial", vm);
            }
        }
    }
}
