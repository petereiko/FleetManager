using FleetManager.Business;
using FleetManager.Business.DataObjects.TripsDto;
using FleetManager.Business.Implementations.TripModule;
using FleetManager.Business.Interfaces.DriverVehicleModule;
using FleetManager.Business.Interfaces.TripModule;
using FleetManager.Business.Interfaces.UserModule;
using FleetManager.Business.ViewModels.TripsViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FleetManager.App.Controllers
{
    [Authorize(Policy = "DriverWeb")]
    public class DriverTripsController : Controller
    {
        private readonly ITripService _tripService;
        private readonly IDriverVehicleService _assignmentService;
        private readonly IAuthUser _authUser;

        public DriverTripsController(ITripService tripService, IDriverVehicleService assignmentService, IAuthUser authUser)
        {
            _tripService = tripService;
            _assignmentService = assignmentService;
            _authUser = authUser;
        }


        // GET: /Driver
        public async Task<IActionResult> Index(int page = 1, int pageSize = 20)
        {
            var driverId = await _assignmentService.GetDriverIdByUserAsync(_authUser.UserId);
            var resp = await _tripService.GetDriverTripsAsync(driverId, page, pageSize);
            if (!resp.Success) TempData["Error"] = resp.Message;
            return View(resp.Result);
        }

        // GET: /Driver/Details/5
        public async Task<IActionResult> Details(long id)
        {
            // Ensure trip belongs to driver
            var driverId = await _assignmentService.GetDriverIdByUserAsync(_authUser.UserId);

            var tripResp = await _tripService.GetTripByIdAsync(id);
            if (!tripResp.Success) return NotFound();

            if (tripResp.Result.DriverId != driverId && !User.IsInRole("Admin"))
                return Forbid();

            return View(tripResp.Result);
        }

        // GET: Start form modal/page
        [HttpGet]
        public async Task<IActionResult> Start(long id)
        {
            var driverId = await _assignmentService.GetDriverIdByUserAsync(_authUser.UserId);

            var tripResp = await _tripService.GetTripByIdAsync(id);
            if (!tripResp.Success) return NotFound();

            if (tripResp.Result.DriverId != driverId && !User.IsInRole("Admin")) return Forbid();

            // Prefill suggestion: use vehicle mileage if available, otherwise use trip's StartOdometer
            var vm = new StartTripViewModel
            {
                TripId = id,
                PreferredStartOdometer = tripResp.Result.VehicleMileage ?? tripResp.Result.StartOdometer ?? 0,
                CurrentVehicleMileage = tripResp.Result.VehicleMileage
            };

            return PartialView("_StartPartial", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Start(StartTripViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    return BadRequest(new { success = false, message = string.Join(" | ", errors) });
                }
                return PartialView("_StartPartial", vm);
            }

            var dto = new StartTripDto
            {
                TripId = vm.TripId,
                StartOdometer = vm.StartOdometer,
                Latitude = vm.Latitude,
                Longitude = vm.Longitude,
                LatitudeAccuracy = vm.LatitudeAccuracy,
                Notes = vm.Notes
            };

            var resp = await _tripService.StartTripAsync(dto);

            if (!resp.Success)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return BadRequest(new { success = false, message = resp.Message });
                }
                ModelState.AddModelError(string.Empty, resp.Message);
                // repopulate UX helpers (in case we want to re-render the partial with the hint)
                var tripInfo = await _tripService.GetTripByIdAsync(vm.TripId);
                if (tripInfo.Success)
                {
                    vm.PreferredStartOdometer = tripInfo.Result.VehicleMileage ?? tripInfo.Result.StartOdometer ?? vm.StartOdometer;
                    vm.CurrentVehicleMileage = tripInfo.Result.VehicleMileage;
                }
                return PartialView("_StartPartial", vm);
            }

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return Json(new { success = true, message = resp.Message });

            TempData["Success"] = resp.Message;
            return RedirectToAction("Details", new { id = vm.TripId });
        }


        // GET Complete
        public async Task<IActionResult> Complete(long id)
        {
            var driverId = await _assignmentService.GetDriverIdByUserAsync(_authUser.UserId);

            var tripResp = await _tripService.GetTripByIdAsync(id);
            if (!tripResp.Success) return NotFound();

            if (tripResp.Result.DriverId != driverId) return Forbid();

            var vm = new CompleteTripViewModel 
            {
                TripId = id,
                //PreferredEndOdometer = tripResp.Result.VehicleMileage ?? tripResp.Result.StartOdometer ?? + 10
            };
            return PartialView("_CompletePartial", vm);
            //return View(vm);
        }

        // POST: Complete (AJAX-friendly)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Complete(CompleteTripViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    return BadRequest(new { success = false, message = string.Join(" | ", errors) });
                }
                return PartialView("_CompletePartial", vm);
            }


            var dto = new CompleteTripDto
            {
                TripId = vm.TripId,
                EndOdometer = vm.EndOdometer,
                ActualFuelCost = vm.ActualFuelCost,
                Latitude = vm.Latitude,
                Longitude = vm.Longitude,
                LatitudeAccuracy = vm.LatitudeAccuracy,
                Notes = vm.Notes
            };

            var resp = await _tripService.CompleteTripAsync(dto);

            if (!resp.Success)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return BadRequest(new { success = false, message = resp.Message });

                ModelState.AddModelError(string.Empty, resp.Message);
                return PartialView("_CompletePartial", vm);
            }

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return Json(new { success = true, message = resp.Message });

            TempData["Success"] = resp.Message;
            return RedirectToAction("Details", new { id = vm.TripId });
        }


    }
}
