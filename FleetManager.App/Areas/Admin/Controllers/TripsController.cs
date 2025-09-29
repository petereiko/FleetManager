using FleetManager.Business.DataObjects.TripsDto;
using FleetManager.Business.Enums;
using FleetManager.Business.Interfaces.TripModule;
using FleetManager.Business.UtilityModels;
using FleetManager.Business.ViewModels.TripsViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FleetManager.App.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class TripsController : Controller
    {
        private readonly ITripService _tripService;
        private readonly ILogger<TripsController> _logger;

        public TripsController(ITripService tripService, ILogger<TripsController> logger)
        {
            _tripService = tripService;
            _logger = logger;
        }

        // GET: Admin/Trips
        public async Task<IActionResult> Index(string search, TripStatus? status, int page = 1, int pageSize = 20)
        {
            var filter = new TripFilterDto
            {
                SearchTerm = search,
                Status = status,
                Page = page,
                PageSize = pageSize
            };

            var resp = await _tripService.GetTripsAsync(filter);
            if (!resp.Success)
            {
                TempData["Error"] = resp.Message;
                return View(new PaginatedResult<TripListDto> { Items = new List<TripListDto>(), Page = page, PageSize = pageSize, TotalCount = 0 });
            }

            return View(resp.Result);
        }

        // GET: Admin/Trips/Details/5
        public async Task<IActionResult> Details(long id)
        {
            var resp = await _tripService.GetTripByIdAsync(id);
            if (!resp.Success) return NotFound();
            return View(resp.Result);
        }

        // GET: Admin/Trips/Create
        public IActionResult Create()
        {
            return View(new CreateTripDto());
        }

        // POST: Admin/Trips/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateTripDto dto)
        {
            if (!ModelState.IsValid) return View(dto);

            var resp = await _tripService.CreateTripAsync(dto);
            if (!resp.Success)
            {
                ModelState.AddModelError(string.Empty, resp.Message);
                return View(dto);
            }

            TempData["Success"] = resp.Message;
            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Trips/Edit/5
        public async Task<IActionResult> Edit(long id)
        {
            var resp = await _tripService.GetTripByIdAsync(id);
            if (!resp.Success) return NotFound();

            var trip = resp.Result;
            var dto = new UpdateTripDto
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

            return View(dto);
        }

        // POST: Admin/Trips/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UpdateTripDto dto)
        {
            if (!ModelState.IsValid) return View(dto);

            var resp = await _tripService.UpdateTripAsync(dto);
            if (!resp.Success)
            {
                ModelState.AddModelError(string.Empty, resp.Message);
                return View(dto);
            }

            TempData["Success"] = resp.Message;
            return RedirectToAction(nameof(Details), new { id = dto.Id });
        }

        // POST: Admin/Trips/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(long id)
        {
            var resp = await _tripService.DeleteTripAsync(id);
            TempData[resp.Success ? "Success" : "Error"] = resp.Message;
            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Trips/Assign/5
        public async Task<IActionResult> Assign(long id)
        {
            var resp = await _tripService.GetTripByIdAsync(id);
            if (!resp.Success) return NotFound();
            var vm = new AssignTripViewModel { Trip = resp.Result };
            return View(vm);
        }

        // POST: Admin/Trips/Assign
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Assign(AssignTripDto dto)
        {
            if (!ModelState.IsValid) return View(dto);

            var resp = await _tripService.AssignTripToDriverAsync(dto);
            if (!resp.Success)
            {
                ModelState.AddModelError(string.Empty, resp.Message);
                var vm = new AssignTripViewModel { Trip = (await _tripService.GetTripByIdAsync(dto.TripId)).Result };
                return View(vm);
            }

            TempData["Success"] = resp.Message;
            return RedirectToAction(nameof(Details), new { id = dto.TripId });
        }

        // POST: Admin/Trips/Unassign/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unassign(long id)
        {
            var resp = await _tripService.UnassignTripAsync(id);
            TempData[resp.Success ? "Success" : "Error"] = resp.Message;
            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: Admin/Trips/Start/5
        public async Task<IActionResult> Start(long id)
        {
            var resp = await _tripService.GetTripByIdAsync(id);
            if (!resp.Success) return NotFound();
            var vm = new StartTripDto { TripId = id, Latitude = 0, Longitude = 0 }; // fill location on client
            return View(vm);
        }

        // POST: Admin/Trips/Start
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Start(StartTripDto dto)
        {
            if (!ModelState.IsValid) return View(dto);
            var resp = await _tripService.StartTripAsync(dto);
            if (!resp.Success) { ModelState.AddModelError(string.Empty, resp.Message); return View(dto); }
            TempData["Success"] = resp.Message;
            return RedirectToAction(nameof(Details), new { id = dto.TripId });
        }

        // GET: Admin/Trips/Complete/5
        public async Task<IActionResult> Complete(long id)
        {
            var resp = await _tripService.GetTripByIdAsync(id);
            if (!resp.Success) return NotFound();
            var vm = new CompleteTripDto { TripId = id };
            return View(vm);
        }

        // POST: Admin/Trips/Complete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Complete(CompleteTripDto dto)
        {
            if (!ModelState.IsValid) return View(dto);
            var resp = await _tripService.CompleteTripAsync(dto);
            if (!resp.Success) { ModelState.AddModelError(string.Empty, resp.Message); return View(dto); }
            TempData["Success"] = resp.Message;
            return RedirectToAction(nameof(Details), new { id = dto.TripId });
        }

        // POST: Admin/Trips/Cancel
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(CancelTripDto dto)
        {
            if (!ModelState.IsValid) { TempData["Error"] = "Invalid cancellation data"; return RedirectToAction(nameof(Details), new { id = dto.TripId }); }
            var resp = await _tripService.CancelTripAsync(dto);
            TempData[resp.Success ? "Success" : "Error"] = resp.Message;
            return RedirectToAction(nameof(Details), new { id = dto.TripId });
        }

        // GET: Admin/Trips/Approve/5
        public async Task<IActionResult> Approve(long id)
        {
            var resp = await _tripService.GetTripByIdAsync(id);
            if (!resp.Success) return NotFound();
            var vm = new ApproveTripDto { TripId = id };
            return View(vm);
        }

        // POST: Admin/Trips/Approve
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(ApproveTripDto dto)
        {
            if (!ModelState.IsValid) return View(dto);
            var resp = await _tripService.ApproveTripAsync(dto);
            if (!resp.Success) { ModelState.AddModelError(string.Empty, resp.Message); return View(dto); }
            TempData["Success"] = resp.Message;
            return RedirectToAction(nameof(Details), new { id = dto.TripId });
        }

        // Expenses
        public async Task<IActionResult> Expenses(long id)
        {
            var resp = await _tripService.GetTripExpensesAsync(id);
            if (!resp.Success) { TempData["Error"] = resp.Message; return RedirectToAction(nameof(Details), new { id }); }
            ViewBag.TripId = id;
            return View(resp.Result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddExpense(CreateTripExpenseDto dto)
        {
            if (!ModelState.IsValid) { TempData["Error"] = "Invalid expense"; return RedirectToAction(nameof(Expenses), new { id = dto.TripId }); }
            var resp = await _tripService.AddTripExpenseAsync(dto);
            TempData[resp.Success ? "Success" : "Error"] = resp.Message;
            return RedirectToAction(nameof(Expenses), new { id = dto.TripId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteExpense(long id, long tripId)
        {
            var resp = await _tripService.DeleteTripExpenseAsync(id);
            TempData[resp.Success ? "Success" : "Error"] = resp.Message;
            return RedirectToAction(nameof(Expenses), new { id = tripId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyExpense(long id, long tripId)
        {
            var resp = await _tripService.VerifyExpenseAsync(id);
            TempData[resp.Success ? "Success" : "Error"] = resp.Message;
            return RedirectToAction(nameof(Expenses), new { id = tripId });
        }

        // Dashboard
        public async Task<IActionResult> Dashboard()
        {
            var resp = await _tripService.GetDashboardDataAsync();
            if (!resp.Success) { TempData["Error"] = resp.Message; return View(new TripDashboardViewModel()); }
            return View(resp.Result);
        }
    }
}
