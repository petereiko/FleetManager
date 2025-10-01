using FleetManager.Business;
using FleetManager.Business.DataObjects.TripsDto;
using FleetManager.Business.Enums;
using FleetManager.Business.Interfaces.TripModule;
using FleetManager.Business.ViewModels.TripsViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FleetManager.App.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class AdminTripsController : Controller
    {
        private readonly ITripService _tripService;

        public AdminTripsController(ITripService tripService)
        {
            _tripService = tripService;
        }

        public async Task<IActionResult> Dashboard()
        {
            var response = await _tripService.GetDashboardDataAsync();
            if (!response.Success) TempData["Error"] = response.Message;
            return View(response.Result);
        }

        public async Task<IActionResult> Index(string search, int page = 1, int pageSize = 20)
        {
            var filter = new TripFilterDto
            {
                SearchTerm = search,
                Page = page,
                PageSize = pageSize
            };

            var resp = await _tripService.GetTripsAsync(filter);
            if (!resp.Success) TempData["Error"] = resp.Message;
            return View(resp.Result);
        }

        public async Task<IActionResult> Details(long id)
        {
            var resp = await _tripService.GetTripByIdAsync(id);
            if (!resp.Success) return NotFound();
            // load expenses + checkpoints if needed via additional service calls or included in TripDetailsViewModel
            return View(resp.Result);
        }

        // GET: Create
        public async Task<IActionResult> Create()
        {
            var driversResp = await _tripService.GetDriversForBranchAsync();
            if (!driversResp.Success) TempData["Error"] = driversResp.Message;

            ViewBag.Drivers = new SelectList(driversResp.Result ?? Enumerable.Empty<SimpleDriverDto>(), "Id", "FullName");
            ViewBag.Vehicles = new SelectList(Enumerable.Empty<SelectListItem>());

            return View(new CreateTripDto
            {
                ScheduledStartDate = DateTime.UtcNow.AddHours(1),
                ScheduledEndDate = DateTime.UtcNow.AddHours(2),
                Priority = TripPriority.Normal
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateTripDto dto)
        {
            if (!ModelState.IsValid)
            {
                var driversResp = await _tripService.GetDriversForBranchAsync();
                ViewBag.Drivers = new SelectList(driversResp.Result ?? Enumerable.Empty<SimpleDriverDto>(), "Id", "FullName", dto.DriverId);
                ViewBag.Vehicles = new SelectList(Enumerable.Empty<SelectListItem>());
                return View(dto);
            }

            var resp = await _tripService.CreateTripAsync(dto);
            if (!resp.Success)
            {
                ModelState.AddModelError(string.Empty, resp.Message);
                var driversResp = await _tripService.GetDriversForBranchAsync();
                ViewBag.Drivers = new SelectList(driversResp.Result ?? Enumerable.Empty<SimpleDriverDto>(), "Id", "FullName", dto.DriverId);
                ViewBag.Vehicles = new SelectList(Enumerable.Empty<SelectListItem>());
                return View(dto);
            }

            TempData["Success"] = resp.Message;
            return RedirectToAction(nameof(Index));
        }

        // GET: Edit
        public async Task<IActionResult> Edit(long id)
        {
            var tripResp = await _tripService.GetTripByIdAsync(id);
            if (!tripResp.Success) return NotFound();

            var trip = tripResp.Result;
            var updateDto = new UpdateTripDto
            {
                Id = trip.Id,
                VehicleId = trip.VehicleId,
                DriverId = trip.DriverId,
                Origin = trip.Origin,
                Destination = trip.Destination,
                Purpose = trip.Purpose,
                Description = trip.Description,
                ScheduledStartDate = trip.ScheduledStartDate,
                ScheduledEndDate = trip.ScheduledEndDate,
                EstimatedDistance = trip.EstimatedDistance,
                EstimatedFuelCost = trip.EstimatedFuelCost,
                Priority = trip.Priority,
                RequiresApproval = trip.RequiresApproval,
                Notes = trip.Notes
            };

            var driversResp = await _tripService.GetDriversForBranchAsync();
            ViewBag.Drivers = new SelectList(driversResp.Result ?? Enumerable.Empty<SimpleDriverDto>(), "Id", "FullName", updateDto.DriverId);

            if (updateDto.DriverId.HasValue)
            {
                var vehiclesResp = await _tripService.GetVehiclesForDriverAsync(updateDto.DriverId.Value, updateDto.ScheduledStartDate, updateDto.ScheduledEndDate, true);
                ViewBag.Vehicles = new SelectList(vehiclesResp.Result ?? Enumerable.Empty<SimpleVehicleDto>(), "Id", "Display", updateDto.VehicleId);
            }
            else
            {
                ViewBag.Vehicles = new SelectList(Enumerable.Empty<SelectListItem>());
            }

            return View(updateDto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UpdateTripDto dto)
        {
            if (!ModelState.IsValid)
            {
                var driversResp = await _tripService.GetDriversForBranchAsync();
                ViewBag.Drivers = new SelectList(driversResp.Result ?? Enumerable.Empty<SimpleDriverDto>(), "Id", "FullName", dto.DriverId);
                ViewBag.Vehicles = new SelectList(Enumerable.Empty<SelectListItem>());
                return View(dto);
            }

            var resp = await _tripService.UpdateTripAsync(dto);
            if (!resp.Success)
            {
                ModelState.AddModelError(string.Empty, resp.Message);
                var driversResp = await _tripService.GetDriversForBranchAsync();
                ViewBag.Drivers = new SelectList(driversResp.Result ?? Enumerable.Empty<SimpleDriverDto>(), "Id", "FullName", dto.DriverId);
                ViewBag.Vehicles = new SelectList(Enumerable.Empty<SelectListItem>());
                return View(dto);
            }

            TempData["Success"] = resp.Message;
            return RedirectToAction(nameof(Details), new { id = dto.Id });
        }

        // GET: Assign
        public async Task<IActionResult> Assign(long id)
        {
            var tripResp = await _tripService.GetTripByIdAsync(id);
            if (!tripResp.Success) return NotFound();

            var driversResp = await _tripService.GetDriversForBranchAsync();
            if (!driversResp.Success) TempData["Error"] = driversResp.Message;

            var vm = new AssignTripViewModel
            {
                TripId = id,
                TripNumber = tripResp.Result.TripNumber,
                Drivers = new SelectList(driversResp.Result ?? Enumerable.Empty<SimpleDriverDto>(), "Id", "FullName")
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Assign(AssignTripViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                var driversResp = await _tripService.GetDriversForBranchAsync();
                vm.Drivers = new SelectList(driversResp.Result ?? Enumerable.Empty<SimpleDriverDto>(), "Id", "FullName", vm.DriverId);
                return View(vm);
            }

            var dto = new AssignTripDto
            {
                TripId = vm.TripId,
                DriverId = vm.DriverId.Value,
                Notes = vm.Notes
            };

            var resp = await _tripService.AssignTripToDriverAsync(dto);
            if (!resp.Success)
            {
                ModelState.AddModelError(string.Empty, resp.Message);
                var driversResp = await _tripService.GetDriversForBranchAsync();
                vm.Drivers = new SelectList(driversResp.Result ?? Enumerable.Empty<SimpleDriverDto>(), "Id", "FullName", vm.DriverId);
                return View(vm);
            }

            TempData["Success"] = resp.Message;
            return RedirectToAction(nameof(Details), new { id = vm.TripId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(long tripId, bool isApproved, string comments)
        {
            var dto = new ApproveTripDto { TripId = tripId, IsApproved = isApproved, Comments = comments };
            var resp = await _tripService.ApproveTripAsync(dto);
            if (!resp.Success) TempData["Error"] = resp.Message;
            else TempData["Success"] = resp.Message;
            return RedirectToAction(nameof(Details), new { id = tripId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(long id)
        {
            var resp = await _tripService.DeleteTripAsync(id);
            if (!resp.Success) TempData["Error"] = resp.Message;
            else TempData["Success"] = resp.Message;
            return RedirectToAction(nameof(Index));
        }

        // JSON endpoints
        [HttpGet]
        public async Task<IActionResult> GetDriversForBranch()
        {
            var resp = await _tripService.GetDriversForBranchAsync();
            if (!resp.Success) return BadRequest(resp.Message);
            return Json(resp.Result);
        }

        [HttpGet]
        public async Task<IActionResult> GetVehiclesForDriver(long driverId, string scheduledStart = null, string scheduledEnd = null, bool excludeOverlap = true)
        {
            DateTime? s = null, e = null;
            if (!string.IsNullOrWhiteSpace(scheduledStart) && DateTime.TryParse(scheduledStart, out var parsedS))
                s = DateTime.SpecifyKind(parsedS, DateTimeKind.Utc);
            if (!string.IsNullOrWhiteSpace(scheduledEnd) && DateTime.TryParse(scheduledEnd, out var parsedE))
                e = DateTime.SpecifyKind(parsedE, DateTimeKind.Utc);

            var resp = await _tripService.GetVehiclesForDriverAsync(driverId, s, e, excludeOverlap);
            if (!resp.Success) return BadRequest(resp.Message);
            return Json(resp.Result);
        }
    }

}

