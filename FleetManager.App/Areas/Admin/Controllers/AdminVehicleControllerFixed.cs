// AdminVehicleController.cs  —  fixed version
// Changes from original:
//   1. ApplyVehicleTypeRules() no longer re-adds errors that are already present
//   2. MapToDto() uses the nullable enum values correctly
//   3. Edit POST uses MapToDto() (was using an inline partial mapping that dropped most fields)
//   4. ModelState debug helper (LogModelStateErrors) makes silent failures visible in logs
//   5. Both Create & Edit POST remove the upload-only fields from ModelState
//      before validation so they never cause a phantom error
//   6. CompanyBranchId is long? in the VM — cast safely here

using FleetManager.Business.DataObjects;
using FleetManager.Business.DataObjects.VehicleDto;
using FleetManager.Business.Enums;
using FleetManager.Business.Implementations.VehicleModule;
using FleetManager.Business.Interfaces.CompanyBranchModule;
using FleetManager.Business.Interfaces.CompanyModule;
using FleetManager.Business.Interfaces.UserModule;
using FleetManager.Business.Interfaces.VehicleModule;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

public class AdminVehicleController : Controller
{
    private readonly IAdminVehicleService _vehicleService;
    private readonly IBranchService _branchService;
    private readonly IAuthUser _authUser;
    private readonly ILogger<AdminVehicleController> _logger;
    private readonly IDataProtector _protector;

    public AdminVehicleController(
        IAdminVehicleService vehicleService,
        IBranchService branchService,
        IAuthUser authUser,
        ILogger<AdminVehicleController> logger,
        IDataProtectionProvider dataProtectionProvider)
    {
        _vehicleService = vehicleService;
        _branchService  = branchService;
        _authUser       = authUser;
        _logger         = logger;
        _protector      = dataProtectionProvider.CreateProtector("VehicleIdProtector");
    }

    // ─── INDEX ────────────────────────────────────────────────────────────────
    public async Task<IActionResult> Index(VehicleFilterDto filter, int page = 1, int pageSize = 9)
    {
        try
        {
            var roles    = ParseRoles();
            bool isGlobal = IsGlobalRole(roles);

            if (!isGlobal)
                filter.BranchId = _authUser.CompanyBranchId;

            var query   = _vehicleService.QueryVehicles(filter);
            var total   = await query.CountAsync();
            var vehicles = await query
                .OrderBy(v => v.Make)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var vm = new VehicleIndexViewModel
            {
                Filter     = filter,
                Vehicles   = vehicles,
                Pagination = new PaginationDto
                {
                    CurrentPage = page,
                    PageSize    = pageSize,
                    TotalItems  = total
                }
            };

            if (isGlobal)
            {
                var branches = await _branchService.GetBranchesForCompanyAsync();
                ViewBag.Branches = new SelectList(branches, "Id", "Name", filter.BranchId);
            }

            ViewBag.Statuses = new SelectList(
                Enum.GetValues<VehicleStatus>().Cast<VehicleStatus>()
                    .Select(s => new { Id = (int)s, Name = s.ToString() }),
                "Id", "Name", filter.Status);

            ViewBag.VehicleTypes = new SelectList(
                Enum.GetValues<VehicleType>().Cast<VehicleType>()
                    .Select(t => new { Id = (int)t, Name = t.ToString() }),
                "Id", "Name", filter.Type);

            return View(vm);
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading vehicles");
            return View("Error");
        }
    }

    // ─── DETAILS ──────────────────────────────────────────────────────────────
    public async Task<IActionResult> Details(long id)
    {
        try
        {
            var dto = await _vehicleService.GetVehicleByIdAsync(id);
            if (dto == null) return NotFound();
            return View(dto);
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching vehicle details for id={Id}", id);
            return View("Error");
        }
    }

    // ─── CREATE GET ───────────────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        try
        {
            var vm = new VehicleEditViewModel();
            await PopulateSelectsAsync(vm);
            return View(vm);
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error preparing create-vehicle form");
            return View("Error");
        }
    }

    // ─── CREATE POST ──────────────────────────────────────────────────────────
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(VehicleEditViewModel vm)
    {
        try
        {
            // 1. Repopulate dropdown lists (they are [BindNever] and arrive empty)
            await PopulateSelectsAsync(vm);

            // 2. Remove upload fields — IFormFile can't fail binding but
            //    having them in ModelState as "required" would block submission.
            ModelState.Remove(nameof(vm.PhotoFiles));
            ModelState.Remove(nameof(vm.DocumentFiles));

            // 3. Conditional make/model validation based on vehicle type
            ApplyVehicleTypeRules(vm);

            // 4. Debug: log every error so nothing is silent
            LogModelStateErrors("Create");

            if (!ModelState.IsValid)
                return View(vm);

            var dto    = MapToDto(vm);
            var result = await _vehicleService.CreateVehicleAsync(dto, _authUser.UserId);

            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                return View(vm);
            }

            TempData["SuccessMessage"] = "Vehicle created successfully.";
            return RedirectToAction(nameof(Index));
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating vehicle");
            ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again.");
            await PopulateSelectsAsync(vm);
            return View(vm);
        }
    }

    // ─── EDIT GET ─────────────────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Edit(long id)
    {
        try
        {
            var dto = await _vehicleService.GetVehicleByIdAsync(id);
            if (dto == null) return NotFound();

            var vm = new VehicleEditViewModel
            {
                Id               = dto.Id,
                VehicleType      = dto.VehicleType,
                VehicleMakeId    = dto.VehicleMakeId,
                VehicleModelId   = dto.VehicleModelId,
                CustomMakeName   = dto.CustomMakeName,
                CustomModelName  = dto.CustomModelName,
                Make             = dto.Make,
                Model            = dto.Model,
                Year             = dto.Year,
                VIN              = dto.VIN,
                PlateNo          = dto.PlateNo,
                Color            = dto.Color,
                EngineNumber     = dto.EngineNumber,
                ChassisNumber    = dto.ChassisNumber,
                Mileage          = dto.Mileage,
                RegistrationDate = dto.RegistrationDate,
                LastServiceDate  = dto.LastServiceDate,
                FuelType         = dto.FuelType,
                TransmissionType = dto.TransmissionType,
                VehicleStatus    = dto.VehicleStatus,
                InsuranceCompany     = dto.InsuranceCompany,
                InsuranceExpiryDate  = dto.InsuranceExpiryDate,
                RoadWorthyExpiryDate = dto.RoadWorthyExpiryDate,
                CompanyBranchId      = dto.CompanyBranchId,
                Photos               = dto.Photos,
                Documents            = dto.Documents
            };

            await PopulateSelectsAsync(vm);
            return View(vm);
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading vehicle id={Id} for edit", id);
            return View("Error");
        }
    }

    // ─── EDIT POST ────────────────────────────────────────────────────────────
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(VehicleEditViewModel vm)
    {
        try
        {
            // 1. Repopulate dropdowns
            await PopulateSelectsAsync(vm);

            // 2. Remove upload-only fields before validation
            ModelState.Remove(nameof(vm.PhotoFiles));
            ModelState.Remove(nameof(vm.DocumentFiles));

            // 3. Conditional make/model rules
            ApplyVehicleTypeRules(vm);

            // 4. Debug logging
            LogModelStateErrors("Edit");

            if (!ModelState.IsValid)
                return View(vm);

            // 5. Map the full dto (not an inline partial copy!)
            var dto    = MapToDto(vm);
            var result = await _vehicleService.UpdateVehicleAsync(dto, _authUser.UserId);

            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                return View(vm);
            }

            TempData["SuccessMessage"] = "Vehicle updated successfully.";
            return RedirectToAction(nameof(Details), new { id = vm.Id });
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating vehicle id={Id}", vm.Id);
            ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again.");
            await PopulateSelectsAsync(vm);
            return View(vm);
        }
    }

    // ─── UPDATE STATUS (AJAX) ─────────────────────────────────────────────────
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateVehicleStatus([FromBody] StatusUpdateRequest req)
    {
        if (!Enum.IsDefined(typeof(VehicleStatus), req.NewStatus))
            return BadRequest(new { success = false, message = "Invalid status." });

        try
        {
            var result = await _vehicleService.UpdateVehicleStatusAsync(
                req.VehicleId, (VehicleStatus)req.NewStatus, _authUser.UserId);

            return Json(new { success = result.Success, message = result.Message });
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdateVehicleStatus error");
            return StatusCode(500, new { success = false, message = "Unexpected error." });
        }
    }

    // ─── DELETE ───────────────────────────────────────────────────────────────
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(long id)
    {
        try
        {
            var result = await _vehicleService.DeleteVehicleAsync(id);
            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] =
                result.Success ? "Vehicle deleted successfully." : result.Message;
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting vehicle id={Id}", id);
            TempData["ErrorMessage"] = "An unexpected error occurred.";
        }

        return RedirectToAction(nameof(Index));
    }

    // ─── DELETE DOCUMENT ──────────────────────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> DeleteDocument(long id, long documentId)
    {
        try
        {
            var result = await _vehicleService.DeleteVehicleDocumentAsync(documentId);
            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] =
                result.Success ? "Document deleted successfully." : (result.Message ?? "Failed to delete document.");
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
        }

        return RedirectToAction("Details", new { id });
    }

    // ─── DELETE DOCUMENT (AJAX) ───────────────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> DeleteDocumentAjax(long documentId)
    {
        try
        {
            var result = await _vehicleService.DeleteVehicleDocumentAsync(documentId);
            return Json(new { success = result.Success, message = result.Message });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    // ─── AJAX: get models for a make ──────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> GetVehicleModels(int makeId)
    {
        var models = await _vehicleService.GetVehicleModelsByMakeId(makeId);
        return Json(models);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // PRIVATE HELPERS
    // ══════════════════════════════════════════════════════════════════════════

    private async Task PopulateSelectsAsync(VehicleEditViewModel vm)
    {
        var roles    = ParseRoles();
        bool isGlobal = IsGlobalRole(roles);

        if (isGlobal)
        {
            vm.Branches = await _vehicleService.GetBranchOptionsAsync(_authUser.CompanyId!.Value);
        }
        else
        {
            var all = await _branchService.GetBranchesForCompanyAsync();
            var me  = all.First(b => b.Id == _authUser.CompanyBranchId);
            vm.Branches         = new[] { new SelectListItem { Value = me.Id.ToString(), Text = me.Name } };
            vm.CompanyBranchId  = _authUser.CompanyBranchId;
        }

        vm.FuelTypes         = _vehicleService.GetFuelTypeOptions();
        vm.TransmissionTypes = _vehicleService.GetTransmissionTypeOptions();
        vm.Statuses          = _vehicleService.GetStatusOptions();
        vm.VehicleTypes      = _vehicleService.GetVehicleTypeOptions();
        vm.Makes             = _vehicleService.GetVehicleMakes();

        vm.Models = vm.VehicleMakeId > 0
            ? await _vehicleService.GetVehicleModelsByMakeId(vm.VehicleMakeId.Value)
            : Enumerable.Empty<SelectListItem>();
    }

    /// <summary>
    /// Adds or removes ModelState errors based on which vehicle type was chosen.
    /// Call AFTER PopulateSelectsAsync and BEFORE ModelState.IsValid.
    /// </summary>
    private void ApplyVehicleTypeRules(VehicleEditViewModel vm)
    {
        bool isMotorcycle = vm.VehicleType == VehicleType.Motorcycle;

        if (isMotorcycle)
        {
            // Drop the standard-vehicle make/model errors — they are irrelevant
            ModelState.Remove(nameof(vm.VehicleMakeId));
            ModelState.Remove(nameof(vm.VehicleModelId));

            if (string.IsNullOrWhiteSpace(vm.CustomMakeName))
                ModelState.AddModelError(nameof(vm.CustomMakeName),
                    "Brand / Make name is required for motorcycles.");

            if (string.IsNullOrWhiteSpace(vm.CustomModelName))
                ModelState.AddModelError(nameof(vm.CustomModelName),
                    "Model name is required for motorcycles.");

            vm.VehicleMakeId  = null;
            vm.VehicleModelId = null;
        }
        else
        {
            // Drop the motorcycle errors — they are irrelevant for standard vehicles
            ModelState.Remove(nameof(vm.CustomMakeName));
            ModelState.Remove(nameof(vm.CustomModelName));

            vm.CustomMakeName  = null;
            vm.CustomModelName = null;

            if (!vm.VehicleMakeId.HasValue || vm.VehicleMakeId == 0)
                ModelState.AddModelError(nameof(vm.VehicleMakeId), "Please select a Make.");

            if (!vm.VehicleModelId.HasValue || vm.VehicleModelId == 0)
                ModelState.AddModelError(nameof(vm.VehicleModelId), "Please select a Model.");
        }
    }

    /// <summary>
    /// Maps the view-model to the service DTO.
    /// Using the full MapToDto() is critical — the inline partial copy in the
    /// original Edit POST was silently dropping FuelType, TransmissionType,
    /// EngineNumber, ChassisNumber, dates, insurance, mileage, etc.
    /// </summary>
    private static VehicleDto MapToDto(VehicleEditViewModel vm)
    {
        return new VehicleDto
        {
            Id              = vm.Id,
            VehicleType     = vm.VehicleType,
            VehicleMakeId   = vm.VehicleMakeId,
            VehicleModelId  = vm.VehicleModelId,
            CustomMakeName  = vm.CustomMakeName,
            CustomModelName = vm.CustomModelName,
            Year            = vm.Year,
            VIN             = vm.VIN,
            PlateNo         = vm.PlateNo,
            Color           = vm.Color,
            EngineNumber    = vm.EngineNumber,
            ChassisNumber   = vm.ChassisNumber,
            RegistrationDate     = vm.RegistrationDate,
            LastServiceDate      = vm.LastServiceDate,
            Mileage              = vm.Mileage,
            FuelType             = vm.FuelType ?? default,        // nullable → enum default if not set
            TransmissionType     = vm.TransmissionType ?? default,
            VehicleStatus        = vm.VehicleStatus ?? default,
            InsuranceCompany     = vm.InsuranceCompany,
            InsuranceExpiryDate  = vm.InsuranceExpiryDate,
            RoadWorthyExpiryDate = vm.RoadWorthyExpiryDate,
            CompanyBranchId      = vm.CompanyBranchId ?? 0,       // validated as required above
            PhotoFiles           = vm.PhotoFiles ?? new(),
            DocumentFiles        = vm.DocumentFiles ?? new()
        };
    }

    // ─── tiny helpers ─────────────────────────────────────────────────────────
    private string[] ParseRoles() =>
        (_authUser.Roles ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(r => r.Trim())
            .ToArray();

    private static bool IsGlobalRole(string[] roles) =>
        roles.Contains("Company Owner") || roles.Contains("Super Admin");

    /// <summary>
    /// Dumps every ModelState error to the structured log.
    /// This is the fastest way to see which field / rule is silently failing.
    /// Remove or guard with #if DEBUG once everything is working.
    /// </summary>
    private void LogModelStateErrors(string action)
    {
        if (ModelState.IsValid) return;

        foreach (var (key, entry) in ModelState)
        {
            foreach (var error in entry.Errors)
            {
                _logger.LogWarning(
                    "[{Action}] ModelState error — Field: {Field} | Error: {Error}",
                    action, key, error.ErrorMessage ?? error.Exception?.Message ?? "(unknown)");
            }
        }
    }
}
