using FleetManager.Business.DataObjects;
using FleetManager.Business.Enums;
using FleetManager.Business.Interfaces.DriverVehicleModule;
using FleetManager.Business.Interfaces.FineAndTollModule;
using FleetManager.Business.Interfaces.UserModule;
using FleetManager.Business.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FleetManager.App.Controllers
{
    [Authorize(Policy = "DriverWeb")]
    public class DriverFineTollController : Controller
    {
        private readonly IFineAndTollService _fineService;
        private readonly IDriverVehicleService _assignmentService;
        private readonly IAuthUser _auth;
        private readonly ILogger<DriverFineTollController> _logger;

        public DriverFineTollController(
            IFineAndTollService fineService,
            IDriverVehicleService assignmentService,
            IAuthUser auth,
            ILogger<DriverFineTollController> logger)
        {
            _fineService = fineService;
            _assignmentService = assignmentService;
            _auth = auth;
            _logger = logger;
        }

        // INDEX
        // ─────────────────────────────────────────────────────────────
        public async Task<IActionResult> Index()
        {
            try
            {
                var driverUserId = _auth.UserId!;
                var list = await _fineService
                    .QueryByDriver(driverUserId)
                    .OrderByDescending(f => f.CreatedDate)
                    .Select(d => new FineTollListItemViewModel
                    {
                        Id = d.Id,
                        DateLogged = d.CreatedDate,
                        Type = d.Type.ToString(),
                        Title = d.Title,
                        Amount = d.Amount,
                        Currency = d.Currency,
                        Status = d.Status.ToString(),
                        VehicleDescription = d.VehicleDescription,
                        IsMinimal = d.IsMinimal
                    })
                    .ToListAsync();

                return View(list);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading fine/toll list");
                return View("Error");
            }
        }

        // DETAILS
        // ─────────────────────────────────────────────────────────────
        public async Task<IActionResult> Details(long id)
        {
            var result = await _fineService.GetByIdAsync(id);

            if (result == null)
                return NotFound();

            return View(result);
        }

        // CREATE (GET)
        // ─────────────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var vm = await BuildInputViewModel();
            return View(vm);
        }

        // CREATE (POST) + FILE UPLOAD
        // ─────────────────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FineTollInputViewModel vm)
        {
            var driverUserId = _auth.UserId!;

            vm = await RebuildDropdowns(vm);

            if (!ModelState.IsValid)
                return View(vm);

            var input = new FineAndTollInputDto
            {
                VehicleId = vm.Input.VehicleId,
                Type = vm.Input.Type,
                Title = vm.Input.Title,
                Amount = vm.Input.Amount,
                Currency = vm.Currency,
                Reason = vm.Input.Reason,
                Notes = vm.Input.Notes,
                IsMinimal = vm.Input.IsMinimal,
                ProofFiles = vm.Input.ProofFiles
            };

            var result = await _fineService.CreateAsync(input, driverUserId);

            if (!result.Success)
            {
                ModelState.AddModelError("", result.Message);
                return View(vm);
            }

            TempData["SuccessMessage"] = "Fine/Toll logged successfully.";
            return RedirectToAction(nameof(Index));
        }

        // EDIT (GET)
        // ─────────────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Edit(long id)
        {
            var result = await _fineService.GetByIdAsync(id);
            if (result == null)
                return NotFound();

            var vm = await BuildInputViewModel();

            vm.Id = result.Id;
            vm.Currency = result.Currency;

            vm.Input = new FineTollRegisterViewModel
            {
                VehicleId = result.VehicleId,
                Type = result.Type,
                Title = result.Title,
                Amount = result.Amount,
                Reason = result.Reason,
                Notes = result.Notes,
                IsMinimal = result.IsMinimal
            };

            return View(vm);
        }

        // EDIT (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(FineTollInputViewModel vm)
        {
            vm = await RebuildDropdowns(vm);

            if (!ModelState.IsValid)
                return View(vm);

            var input = new FineAndTollInputDto
            {
                VehicleId = vm.Input.VehicleId,
                Type = vm.Input.Type,
                Title = vm.Input.Title,
                Amount = vm.Input.Amount,
                Currency = vm.Currency,
                Reason = vm.Input.Reason,
                Notes = vm.Input.Notes,
                IsMinimal = vm.Input.IsMinimal,
                ProofFiles = vm.Input.ProofFiles
            };

            var result = await _fineService.UpdateAsync(vm.Id!.Value, input, _auth.UserId!);

            if (!result.Success)
            {
                ModelState.AddModelError("", result.Message);
                return View(vm);
            }

            TempData["SuccessMessage"] = "Fine/Toll updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        // DELETE
        // ─────────────────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(long id)
        {
            var userId = _auth.UserId;
            await _fineService.DeleteAsync(id,userId);
            TempData["SuccessMessage"] = "Record deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        // HELPERS
        // ─────────────────────────────────────────────────────────────
        private async Task<FineTollInputViewModel> BuildInputViewModel()
        {
            var driverUserId = _auth.UserId!;
            var driverId = await _assignmentService.GetDriverIdByUserAsync(driverUserId);

            var vehicles = await _assignmentService
                .QueryAssignmentsByDriver(driverId)
                .Select(a => new SelectListItem(a.VehicleMakeModel, a.VehicleId.ToString()))
                .ToListAsync();

            return new FineTollInputViewModel
            {
                Currency = "NGN",
                DateLogged = DateTime.Today,
                Vehicles = vehicles,
                Types = Enum.GetValues<FineTollType>()
                    .Select(e => new SelectListItem(e.ToString(), ((int)e).ToString()))
                    .ToList(),
                Input = new FineTollRegisterViewModel()
            };
        }

        private async Task<FineTollInputViewModel> RebuildDropdowns(FineTollInputViewModel vm)
        {
            var rebuilt = await BuildInputViewModel();
            vm.Vehicles = rebuilt.Vehicles;
            vm.Types = rebuilt.Types;
            return vm;
        }
    }





    
}

// ─── DriverFineTollController ─────────────────────────────────────────────────
//[Authorize(Policy = "DriverWeb")]
//public class DriverFineTollController : Controller
//{
//    private readonly IFineAndTollService _fineService;
//    private readonly IDriverVehicleService _assignmentService;
//    private readonly IAuthUser _auth;
//    private readonly ILogger<DriverFineTollController> _logger;

//    public DriverFineTollController(
//        IFineAndTollService fineService,
//        IDriverVehicleService assignmentService,
//        IAuthUser auth,
//        ILogger<DriverFineTollController> logger)
//    {
//        _fineService = fineService;
//        _assignmentService = assignmentService;
//        _auth = auth;
//        _logger = logger;
//    }

//    // ─── INDEX ────────────────────────────────────────────────────────────────
//    public async Task<IActionResult> Index()
//    {
//        try
//        {
//            // driverUserId is simply the authenticated user's ID
//            var driverUserId = _auth.UserId!;
//            var list = await _fineService
//                .QueryByDriver(driverUserId)
//                .OrderByDescending(f => f.CreatedDate)
//                .Select(d => new FineTollListItemViewModel
//                {
//                    Id = d.Id,
//                    DateLogged = d.CreatedDate,
//                    Type = d.Type.ToString(),
//                    Title = d.Title,
//                    Amount = d.Amount,
//                    Currency = d.Currency,
//                    Status = d.Status.ToString(),
//                    VehicleDescription = d.VehicleDescription,
//                    IsMinimal = d.IsMinimal
//                })
//                .ToListAsync();

//            return View(list);
//        }
//        catch (UnauthorizedAccessException)
//        {
//            return Forbid();
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "Error loading fine/toll list for driver");
//            return View("Error");
//        }
//    }

//    // ─── GET: CREATE ─────────────────────────────────────────────────────────
//    [HttpGet]
//    public async Task<IActionResult> Create()
//    {
//        // build the form model
//        var driverUserId = _auth.UserId!;
//        var driverId = await _assignmentService.GetDriverIdByUserAsync(driverUserId);

//        var vehicles = await _assignmentService
//            .QueryAssignmentsByDriver(driverId)
//            .Select(a => new SelectListItem(a.VehicleMakeModel, a.VehicleId.ToString()))
//            .ToListAsync();

//        var vm = new FineTollInputViewModel
//        {
//            DateLogged = DateTime.Today,
//            Vehicles = vehicles,
//            Types = Enum.GetValues<FineTollType>()
//                               .Cast<FineTollType>()
//                               .Select(e => new SelectListItem(e.ToString(), ((int)e).ToString()))
//                               .ToList(),
//            Currency = "NGN"

//        };

//        return View(vm);
//    }

//    // ─── POST: CREATE ────────────────────────────────────────────────────────
//    [HttpPost, ValidateAntiForgeryToken]
//    public async Task<IActionResult> Create(FineTollInputViewModel vm)
//    {
//        var driverUserId = _auth.UserId!;
//        var driverId = await _assignmentService.GetDriverIdByUserAsync(driverUserId);

//        // repopulate dropdowns
//        vm.Vehicles = await _assignmentService
//            .QueryAssignmentsByDriver(driverId)
//            .Select(a => new SelectListItem(a.VehicleMakeModel, a.VehicleId.ToString(), a.VehicleId == vm.Input.VehicleId))
//            .ToListAsync();
//        vm.Types = Enum.GetValues<FineTollType>()
//                      .Cast<FineTollType>()
//                      .Select(e => new SelectListItem(
//                          text: e.ToString(),
//                          value: ((int)e).ToString(),
//                          selected: e == vm.Input.Type))
//                      .ToList();

//        if (!ModelState.IsValid)
//            return View(vm);

//        var input = new FineAndTollInputDto
//        {
//            VehicleId = vm.Input.VehicleId,
//            Type = vm.Input.Type,
//            Title = vm.Input.Title,
//            Amount = vm.Input.Amount,
//            Currency = vm.Currency,
//            Reason = vm.Input.Reason,
//            Notes = vm.Input.Notes,
//            IsMinimal = vm.Input.IsMinimal
//        };

//        var result = await _fineService.CreateAsync(input, driverUserId);
//        if (!result.Success)
//        {
//            ModelState.AddModelError("", result.Message);
//            return View(vm);
//        }

//        TempData["SuccessMessage"] = "Fine/Toll logged successfully.";
//        return RedirectToAction(nameof(Index));
//    }
//}
