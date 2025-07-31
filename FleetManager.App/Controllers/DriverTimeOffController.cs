using FleetManager.Business.DataObjects.Schedule;
using FleetManager.Business.Enums;
using FleetManager.Business.Interfaces.DriverVehicleModule;
using FleetManager.Business.Interfaces.ScheduleModule;
using FleetManager.Business.Interfaces.UserModule;
using FleetManager.Business.ViewModels.Schedule;
using Microsoft.AspNetCore.Mvc;

namespace FleetManager.App.Controllers
{
    public class DriverTimeOffController : Controller
    {
        private readonly ITimeOffService _timeOff;
        private readonly IAuthUser _auth;
        private readonly IDriverVehicleService _assignmentService;
        private readonly ILogger<DriverTimeOffController> _logger;

        public DriverTimeOffController(
            ITimeOffService timeOffService,
            IAuthUser authUser,
            ILogger<DriverTimeOffController> logger,
            IDriverVehicleService assignmentService)
        {
            _timeOff = timeOffService;
            _auth = authUser;
            _logger = logger;
            _assignmentService = assignmentService;
        }

        // GET: /User/DriverTimeOff
        public async Task<IActionResult> Index()
        {
            try
            {
                var driverId = await _assignmentService.GetDriverIdByUserAsync(_auth.UserId!);
                var requests = await _timeOff.GetRequestsByDriverAsync(driverId);
                var categories = await _timeOff.GetCategoriesAsync();
                var vm = new DriverTimeOffIndexViewModel
                { 
                    Requests = requests,
                    Categories = categories
                };
                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading time‑off list");
                return View("Error");
            }
        }

        // GET: /User/DriverTimeOff/Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var vm = new TimeOffCreateViewModel
            {
                StartDate = DateTime.Today,
                EndDate = DateTime.Today,
                Categories = await _timeOff.GetCategoriesAsync()
            };
            return View(vm);
        }

        // POST: /User/DriverTimeOff/Create
        //[HttpPost, ValidateAntiForgeryToken]
        //public async Task<IActionResult> Create(TimeOffCreateViewModel vm)
        //{
        //    // repopulate categories on failure
        //    vm.Categories = await _timeOff.GetCategoriesAsync();

        //    if (!ModelState.IsValid)
        //        return View(vm);

        //    try
        //    {
        //        var driverId = await _assignmentService.GetDriverIdByUserAsync(_auth.UserId!);
        //        var dto = new TimeOffRequestDto
        //        {
        //            DriverId = driverId,
        //            CategoryId = vm.CategoryId,
        //            StartDate = vm.StartDate,
        //            EndDate = vm.EndDate,
        //            Reason = vm.Reason,
        //            Status = TimeOffStatus.Pending,
        //            CompanyBranchId = _auth.CompanyBranchId!.Value
        //        };
        //        await _timeOff.CreateRequestAsync(dto);

        //        TempData["SuccessMessage"] = "Time‑off request submitted.";
        //        return RedirectToAction(nameof(Index));
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error creating time‑off request");
        //        ModelState.AddModelError("", "An error occurred submitting your request.");
        //        return View(vm);
        //    }
        //}


        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TimeOffCreateViewModel vm)
        {
            vm.Categories = await _timeOff.GetCategoriesAsync();

            if (!ModelState.IsValid)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return BadRequest("Validation failed");
                return View(vm);
            }

            try
            {
                var driverId = await _assignmentService.GetDriverIdByUserAsync(_auth.UserId!);
                var dto = new TimeOffRequestDto
                {
                    DriverId = driverId,
                    CategoryId = vm.CategoryId,
                    StartDate = vm.StartDate,
                    EndDate = vm.EndDate,
                    Reason = vm.Reason,
                    Status = TimeOffStatus.Pending,
                    CompanyBranchId = _auth.CompanyBranchId!.Value
                };
                await _timeOff.CreateRequestAsync(dto);

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = true });

                TempData["SuccessMessage"] = "Time-off request submitted.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating time-off request");
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return StatusCode(500, "An error occurred submitting your request.");
                ModelState.AddModelError("", "An error occurred submitting your request.");
                return View(vm);
            }
        }


        // GET: /User/DriverTimeOff/Details/5
        public async Task<IActionResult> Details(long id)
        {
            try
            {
                var req = await _timeOff.GetRequestByIdAsync(id);
                if (req == null || req.DriverId != (await _assignmentService.GetDriverIdByUserAsync(_auth.UserId!)))
                    return NotFound();

                var vm = new TimeOffDetailsViewModel { Request = req };
                return PartialView("_DetailsPartial", vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading time‑off details #{Id}", id);
                return View("Error");
            }
        }
    }
}
