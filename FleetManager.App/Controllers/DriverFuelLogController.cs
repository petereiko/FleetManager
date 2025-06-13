using FleetManager.Business.DataObjects;
using FleetManager.Business.Interfaces.DriverVehicleModule;
using FleetManager.Business.Interfaces.FuelLogModule;
using FleetManager.Business.Interfaces.UserModule;
using FleetManager.Business.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FleetManager.App.Controllers
{
    [Authorize(Roles = "Driver")]
    public class DriverFuelLogController : Controller
    {
        private readonly IFuelLogService _fuelLogService;
        private readonly IDriverVehicleService _assignmentService;
        private readonly IAuthUser _auth;
        private readonly ILogger<DriverFuelLogController> _logger;

        public DriverFuelLogController(
            IFuelLogService fuelLogService,
            IDriverVehicleService assignmentService,
            IAuthUser auth,
            ILogger<DriverFuelLogController> logger)
        {
            _fuelLogService = fuelLogService;
            _assignmentService = assignmentService;
            _auth = auth;
            _logger = logger;
        }

       

        // ─── INDEX ────────────────────────────────────────────────────────────────
        public async Task<IActionResult> Index()
        {
            try
            {
                var driverId = await _assignmentService.GetDriverIdByUserAsync(_auth.UserId);
                var logs = await _fuelLogService
                    .QueryByDriver(driverId)
                    .OrderByDescending(f => f.Date)
                    .ToListAsync();

                return View(logs);
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        // ─── GET: Create ─────────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var driverId = await _assignmentService.GetDriverIdByUserAsync(_auth.UserId);

            // get vehicles assigned to this driver
            var assignments = _assignmentService
                .QueryAssignmentsByDriver(driverId);

            var vm = new FuelLogInputViewModel
            {
                Date = DateTime.Today,
                FuelTypes = _fuelLogService.GetFuelTypeOptions(),
                Vehicles = await assignments.Select(a => new SelectListItem(a.VehicleMakeModel, a.VehicleId.ToString())).ToListAsync()
            };

            return View(vm);
        }

        // ─── POST: Create ────────────────────────────────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FuelLogInputViewModel vm)
        {
            var driverId = await _assignmentService.GetDriverIdByUserAsync(_auth.UserId);

            // repopulate vehicles
            vm.Vehicles = await _assignmentService
                .QueryAssignmentsByDriver(driverId)
                .Select(a => new SelectListItem(a.VehicleMakeModel, a.VehicleId.ToString(), a.VehicleId == vm.VehicleId))
                .ToListAsync();

            if (!ModelState.IsValid)
                return View(vm);

            var input = new FuelLogInputDto
            {
                DriverId = driverId,
                VehicleId = vm.VehicleId,
                Date = vm.Date,
                Odometer = vm.Odometer,
                Volume = vm.Volume,
                Cost = vm.Cost,
                FuelType = vm.FuelType,
                Notes = vm.Notes,
                ReceiptFile = vm.ReceiptFile
            };

            var result = await _fuelLogService.CreateAsync(input, _auth.UserId);
            if (!result.Success)
            {
                ModelState.AddModelError("", result.Message);
                return View(vm);
            }

            TempData["SuccessMessage"] = "Fuel log created.";
            return RedirectToAction(nameof(Index));
        }

        // ─── GET: Edit ───────────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Edit(long id)
        {
            var dto = await _fuelLogService.GetByIdAsync(id);
            if (dto == null) return NotFound();

            var driverId = await _assignmentService.GetDriverIdByUserAsync(_auth.UserId);
            if (dto.DriverId != driverId) return Forbid();

            var vm = new FuelLogInputViewModel
            {
                Id = dto.Id,
                VehicleId = dto.VehicleId,
                Date = dto.Date,
                Odometer = dto.Odometer,
                Volume = dto.Volume,
                Cost = dto.Cost,
                FuelType = dto.FuelType,
                Notes = dto.Notes,
                ExistingReceiptPath = dto.ReceiptPath
            };

            // populate vehicles
            vm.Vehicles = await _assignmentService
                .QueryAssignmentsByDriver(driverId)
                .Select(a => new SelectListItem(a.VehicleMakeModel,a.VehicleId.ToString(), a.VehicleId == vm.VehicleId))
                .ToListAsync();

            return View(vm);
        }

        // ─── POST: Edit ──────────────────────────────────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(FuelLogInputViewModel vm)
        {
            var driverId = await _assignmentService.GetDriverIdByUserAsync(_auth.UserId);


            
            vm.Vehicles = await _assignmentService
                .QueryAssignmentsByDriver(driverId)
                .Select(a => new SelectListItem( a.VehicleMakeModel,a.VehicleId.ToString(),a.VehicleId == vm.VehicleId))
                .ToListAsync();

            if (!ModelState.IsValid)
                return View(vm);

            var input = new FuelLogInputDto
            {
                DriverId = driverId,
                VehicleId = vm.VehicleId,
                Date = vm.Date,
                Odometer = vm.Odometer,
                Volume = vm.Volume,
                Cost = vm.Cost,
                FuelType = vm.FuelType,
                Notes = vm.Notes,
                ReceiptFile = vm.ReceiptFile
            };

            var result = await _fuelLogService.UpdateAsync(vm.Id!.Value, input, _auth.UserId);
            if (!result.Success)
            {
                ModelState.AddModelError("", result.Message);
                return View(vm);
            }

            TempData["SuccessMessage"] = "Fuel log updated.";
            return RedirectToAction(nameof(Index));
        }

        // ─── POST: Delete ────────────────────────────────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(long id)
        {
            var driverId = await _assignmentService.GetDriverIdByUserAsync(_auth.UserId);
            var dto = await _fuelLogService.GetByIdAsync(id);
            if (dto?.DriverId != driverId) return Forbid();

            var result = await _fuelLogService.DeleteAsync(id);
            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
            return RedirectToAction(nameof(Index));
        }
    }

}
