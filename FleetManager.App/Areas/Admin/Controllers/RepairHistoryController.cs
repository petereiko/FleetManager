using FleetManager.App.Models;
using FleetManager.Business.DataObjects.RepairHistoryDto;
using FleetManager.Business.DataObjects.VehicleDto;
using FleetManager.Business.Enums;
using FleetManager.Business.Interfaces.DriverVehicleModule;
using FleetManager.Business.Interfaces.ManageDriverModule;
using FleetManager.Business.Interfaces.RepairModule;
using FleetManager.Business.Interfaces.UserModule;
using FleetManager.Business.Interfaces.VehicleModule;
using FleetManager.Business.ViewModels;
using FleetManager.Business.ViewModels.RepairHistoryViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Diagnostics;

[Area("Admin")]
public class RepairHistoryController : Controller
{
    private readonly IRepairService _service;
    private readonly IAuthUser _auth;
    private readonly ILogger<RepairHistoryController> _logger;
    private readonly IDriverVehicleService _assignmentService;
    private readonly IAdminVehicleService _vehicleService;
    private readonly IManageDriverService _driverService;

    public RepairHistoryController( IRepairService svc, IAuthUser auth, ILogger<RepairHistoryController> logger, IDriverVehicleService assignmentService,IManageDriverService driverService, IAdminVehicleService vehicleService)
    {
        _service = svc;
        _auth = auth;
        _logger = logger;
        _assignmentService = assignmentService;
        _driverService = driverService;
        _vehicleService = vehicleService;
    }

    // ─── REPAIRS ─────────────────────────────────────────────────────────────

    public async Task<IActionResult> Index(
        string CurrentFilter = "ByBranch",
        long? DriverId = null,
        long? VehicleId = null,
        int page = 1,
        int pageSize = 20)
    {
        try
        {
            var resp = CurrentFilter switch
            {
                "ByDriver" => await _service.QueryRepairsByBranchAsync(page, pageSize, _auth.CompanyBranchId), // fallback: branch
                "ByVehicle" => await _service.QueryRepairsByVehicleAsync(page, pageSize, VehicleId ?? 0),
                _ => await _service.QueryRepairsByBranchAsync(page, pageSize, _auth.CompanyBranchId)
            };

            if (!resp.Success)
            {
                TempData["ErrorMessage"] = resp.Message;
                return View("Error");
            }

            var repairsPage = resp.Result;

            var vehicles = _vehicleService.QueryVehicles(new VehicleFilterDto { BranchId = _auth.CompanyBranchId });
            var vehicleSelect = vehicles
                .OrderBy(v => v.PlateNo)
                .Select(v => new SelectListItem($"{v.PlateNo} — {v.Make} {v.Model}", v.Id.ToString()))
                .ToList();

            var drivers = _driverService.QueryDriversForBranch(_auth.CompanyBranchId);
            var driverSelect = drivers
                .OrderBy(d => d.FullName)
                .Select(d => new SelectListItem(d.FullName, d.Id.ToString()))
                .ToList();

            var vm = new RepairListViewModel
            {
                Repairs = repairsPage.Items,
                Pagination = new PaginationDto
                {
                    CurrentPage = repairsPage.Page,
                    PageSize = repairsPage.PageSize,
                    TotalItems = repairsPage.TotalPages
                },
                CurrentFilter = CurrentFilter,
                DriverId = DriverId,
                VehicleId = VehicleId,
                Drivers = driverSelect,
                Vehicles = vehicleSelect,
                EditModel = new RepairStatusEditViewModel()
            };

            return View(vm);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading repairs index");
            return View("Error", new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        try
        {
            var drivers = await _driverService.GetDriversForBranchAsync();
            var driverList = drivers.Select(d => new SelectListItem(d.FullName, d.Id.ToString())).ToList();

            var vehicles = await GetVehiclesForCurrentBranchAsync();
            var vehicleList = vehicles.Select(v => new SelectListItem($"{v.Make} {v.Model} ({v.PlateNo})", v.Id.ToString())).ToList();

            var categories = await _service.GetPartCategoriesAsync();
            var priorities = _service.GetPriorityTypeOptions();

            var vm = new RepairCreateViewModel
            {
                Drivers = driverList,
                Vehicles = vehicleList,
                PartCategories = categories,
                Priorities = priorities,
                Items = new List<RepairItemInputViewModel> { new RepairItemInputViewModel() } // one empty row
            };
            return View(vm);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RepairCreateViewModel vm)
    {
        try
        {
            // always reload dropdowns when returning
            var drivers = await _driverService.GetDriversForBranchAsync();
            vm.Drivers = drivers.Select(d => new SelectListItem(d.FullName, d.Id.ToString())).ToList();

            var vehicles = await GetVehiclesForCurrentBranchAsync();
            vm.Vehicles = vehicles.Select(v => new SelectListItem($"{v.Make} {v.Model} ({v.PlateNo})", v.Id.ToString())).ToList();

            vm.PartCategories = await _service.GetPartCategoriesAsync();
            vm.Priorities = _service.GetPriorityTypeOptions();

            if (!ModelState.IsValid) return View(vm);

            var input = new RepairInputDto
            {
                VehicleId = vm.VehicleId,
                DriverId = vm.DriverId,
                Subject = vm.Subject,
                Notes = vm.Notes,
                Priority = vm.Priority,
                Items = vm.Items.Select(i => new RepairItemInputDto
                {
                    PartCategoryId = i.PartCategoryId,
                    PartId = i.PartId,
                    CustomDescription = i.CustomDescription,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice
                }).ToList()
            };

            var resp = await _service.CreateRepairAsync(input, _auth.UserId);
            if (!resp.Success)
            {
                ModelState.AddModelError("", resp.Message);
                return View(vm);
            }

            TempData["SuccessMessage"] = "Repair logged successfully.";
            return RedirectToAction(nameof(Index));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            TempData["Error"] = "Error creating repair log. Please try again.";
            _logger.LogError(ex, "Error creating repair");
            ModelState.AddModelError("", "Unexpected error.");
            return View(vm);
        }
    }

    // Details page - shows repair and allows dynamic invoice load via JSON
    public async Task<IActionResult> Details(long id)
    {
        try
        {
            var repair = await _service.GetRepairByIdAsync(id);
            if (repair == null) return NotFound();
            return View(repair);
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading repair details #{RepairId}", id);
            return View("Error");
        }
    }

    // Edit (simple editable fields: Subject, Notes, Priority)
    [HttpGet]
    public async Task<IActionResult> Edit(long id)
    {
        try
        {
            // get repair full DTO (includes Items with PartCategoryId and PartId)
            var r = await _service.GetRepairByIdAsync(id);
            if (r == null) return NotFound();

            // load dropdowns
            var drivers = await _driverService.GetDriversForBranchAsync();
            var vehicles = await GetVehiclesForCurrentBranchAsync();
            var partCategories = await _service.GetPartCategoriesAsync();
            var priorities = _service.GetPriorityTypeOptions();

            // map service DTO -> Edit ViewModel
            var vm = new RepairEditViewModel
            {
                Id = r.Id,
                VehicleId = r.VehicleId,
                DriverId = r.DriverId,
                Subject = r.Subject,
                Notes = r.Notes,
                Priority = r.Priority,
                Drivers = drivers.Select(d => new SelectListItem(d.FullName, d.Id.ToString())).ToList(),
                Vehicles = vehicles.Select(v => new SelectListItem($"{v.Make} {v.Model} ({v.PlateNo})", v.Id.ToString())).ToList(),
                PartCategories = partCategories,
                Priorities = priorities,
                Items = r.Items.Select(i => new RepairItemInputViewModel
                {
                    Id = i.Id,
                    PartCategoryId = i.PartId.HasValue ? i.PartId == null ? i.PartCategoryId : i.PartCategoryId : i.PartCategoryId,
                    PartId = i.PartId,
                    CustomDescription = i.CustomDescription,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice
                }).ToList()
            };

            // Preload parts for each existing item category so the Part <select> for each row is populated on page load.
            // We'll build a dictionary keyed by item index (0,1,2...) to a list of SelectListItem.
            var partsByRow = new Dictionary<int, List<SelectListItem>>();
            for (int idx = 0; idx < vm.Items.Count; idx++)
            {
                var catId = vm.Items[idx].PartCategoryId;
                if (catId.HasValue)
                {
                    var parts = await _service.GetPartsByCategoryAsync(catId.Value);
                    partsByRow[idx] = parts;
                }
                else
                {
                    partsByRow[idx] = new List<SelectListItem>();
                }
            }

            // Put JSON into ViewBag for use by client-side script
            ViewBag.ItemPartsJson = System.Text.Json.JsonSerializer.Serialize(partsByRow);

            return View(vm);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading repair edit #{Id}", id);
            return View("Error");
        }
    }


    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(RepairEditViewModel vm)
    {
        try
        {
            // reload dropdowns for return-case
            var drivers = await _driverService.GetDriversForBranchAsync();
            vm.Drivers = drivers.Select(d => new SelectListItem(d.FullName, d.Id.ToString())).ToList();
            var vehicles = await GetVehiclesForCurrentBranchAsync();
            vm.Vehicles = vehicles.Select(v => new SelectListItem($"{v.Make} {v.Model} ({v.PlateNo})", v.Id.ToString())).ToList();
            vm.PartCategories = await _service.GetPartCategoriesAsync();
            vm.Priorities = _service.GetPriorityTypeOptions();

            if (!ModelState.IsValid)
                return View(vm);

            var input = new UpdateRepairInputDto
            {
                RepairId = vm.Id,
                VehicleId = vm.VehicleId,
                DriverId = vm.DriverId,
                Subject = vm.Subject,
                Notes = vm.Notes,
                Priority = vm.Priority,
                Items = vm.Items.Select(i => new RepairItemUpdateDto
                {
                    Id = i.Id,
                    PartCategoryId = i.PartCategoryId,
                    PartId = i.PartId,
                    CustomDescription = i.CustomDescription,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice
                }).ToList()
            };

            var resp = await _service.UpdateRepairAsync(input);

            if (!resp.Success)
            {
                ModelState.AddModelError("", resp.Message);
                return View(vm);
            }

            TempData["SuccessMessage"] = "Repair updated successfully.";
            return RedirectToAction(nameof(Details), new { id = vm.Id });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error editing repair #{Id}", vm.Id);
            ModelState.AddModelError("", "Unexpected error.");
            return View(vm);
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateRepairStatus(RepairStatusEditViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Where(kvp => kvp.Value.Errors.Any())
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray());
            return Json(new { success = false, message = "Validation failed", errors });
        }

        var input = new UpdateRepairStatusDto
        {
            RepairId = vm.RepairId,
            NewStatus = vm.NewStatus,
            InvoiceStatus = vm.NewInvoiceStatus,
            AdminNotes = vm.AdminNotes
        };

        var resp = await _service.UpdateRepairStatusAsync(input);
        if (!resp.Success) return Json(new { success = false, message = resp.Message });

        return Json(new
        {
            success = true,
            repairId = vm.RepairId,
            newStatus = vm.NewStatus.ToString(),
            newInvoiceStatus = vm.NewInvoiceStatus?.ToString(),
            message = "Status updated successfully",
            updatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        });
    }


    // GET: /Admin/ManageRepair/VehicleRepairs?vehicleId=123&page=1
    [HttpGet]
    public async Task<IActionResult> VehicleRepairs(long vehicleId, int page = 1, int pageSize = 20)
    {
        try
        {
            // Query service for repairs on the vehicle
            var resp = await _service.QueryRepairsByVehicleAsync(page, pageSize, vehicleId);
            if (!resp.Success)
            {
                TempData["ErrorMessage"] = resp.Message;
                return View("Error");
            }

            var paged = resp.Result;

            // Try to load a friendly vehicle title. Use your vehicle service if it exposes a GetVehicleByIdAsync method.
            string vehicleTitle = paged.Items.FirstOrDefault()?.VehicleDescription ?? $"Vehicle #{vehicleId}";

            try
            {
                // best-effort: check if vehicle service has GetVehicleByIdAsync (some projects use different names)
                // If your IAdminVehicleService exposes GetVehicleByIdAsync or GetVehicleAsync change appropriately.
                var veh = await _vehicleService.GetVehicleByIdAsync(vehicleId);
                if (veh != null)
                {
                    // adapt depending on your vehicle DTO shape; using PlateNo + Make + Model if available
                    var make = (veh.Make ) ?? "";
                    var model = (veh.Model ) ?? "";
                    var plate = veh.PlateNo ?? "";
                    vehicleTitle = $" {make} {model} - {plate} ".Trim();
                }
            }
            catch
            {
                // not critical — just keep earlier fallback
            }

            var vm = new RepairHistoryViewModel
            {
                VehicleId = vehicleId,
                VehicleDescription = vehicleTitle,
                Repairs = paged.Items,
                Pagination = new PaginationDto
                {
                    CurrentPage = paged.Page,
                    PageSize = paged.PageSize,
                    TotalItems = paged.TotalPages // keep consistent with how you used PaginationDto elsewhere
                }
            };

            return View(vm);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading repair history for vehicle {VehicleId}", vehicleId);
            return View("Error", new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }


    // ─── INVOICES ────────────────────────────────────────────────────────────

    public async Task<IActionResult> Invoices(int page = 1, int pageSize = 20)
    {
        try
        {
            var driverId = await _assignmentService.GetDriverIdByUserAsync(_auth.UserId!);
            var resp = await _service.QueryRepairInvoicesByBranchAsync(page, pageSize, _auth.CompanyBranchId);
            if (!resp.Success)
            {
                TempData["ErrorMessage"] = resp.Message;
                return View("Error");
            }

            var paged = resp.Result;
            var vm = new InvoiceListViewModel
            {
                Invoices = paged.Items,
                Pagination = new PaginationDto
                {
                    CurrentPage = paged.Page,
                    PageSize = paged.PageSize,
                    TotalItems = paged.TotalPages
                }
            };
            return View(vm);
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading repair invoices");
            return View("Error");
        }
    }

    public async Task<IActionResult> InvoiceDetails(long id)
    {
        try
        {
            var inv = await _service.GetRepairInvoiceByIdAsync(id);
            if (inv == null) return NotFound();
            return View(inv);
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading repair invoice #{InvoiceId}", id);
            return View("Error");
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateInvoiceStatus(long invoiceId, InvoiceStatus newStatus)
    {
        try
        {
            var resp = await _service.UpdateRepairInvoiceStatusAsync(invoiceId, newStatus);
            TempData[resp.Success ? "SuccessMessage" : "ErrorMessage"] = resp.Message;
            return RedirectToAction(nameof(InvoiceDetails), new { id = invoiceId });
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating repair invoice status #{InvoiceId}", invoiceId);
            TempData["ErrorMessage"] = "Unexpected error.";
            return RedirectToAction(nameof(InvoiceDetails), new { id = invoiceId });
        }
    }

    // JSON helper used by Details page to fetch invoice dynamically
    [HttpGet]
    public async Task<IActionResult> GetRepairInvoiceJson(long repairId)
    {
        var r = await _service.GetRepairByIdAsync(repairId);
        if (r == null) return Json(new { success = false, message = "Repair not found" });

        if (r.Invoice == null) return Json(new { success = false, message = "No invoice for this repair" });

        return Json(new { success = true, invoice = r.Invoice });
    }

    [HttpGet]
    public async Task<IActionResult> GetPartsByCategory(int categoryId)
    {
        var models = await _service.GetPartsByCategoryAsync(categoryId);
        return Json(models);
    }

    private async Task<List<VehicleListItemDto>> GetVehiclesForCurrentBranchAsync()
    {
        return await _vehicleService.GetVehiclesAsync(new VehicleFilterDto { BranchId = _auth.CompanyBranchId });
    }
}
