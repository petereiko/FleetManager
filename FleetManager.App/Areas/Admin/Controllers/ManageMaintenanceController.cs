using FleetManager.App.Models;
using FleetManager.Business.DataObjects.MaintenanceDto;
using FleetManager.Business.DataObjects.VehicleDto;
using FleetManager.Business.Enums;
using FleetManager.Business.Interfaces.DriverVehicleModule;
using FleetManager.Business.Interfaces.MaintenanceModule;
using FleetManager.Business.Interfaces.ManageDriverModule;
using FleetManager.Business.Interfaces.UserModule;
using FleetManager.Business.Interfaces.VehicleModule;
using FleetManager.Business.UtilityModels;
using FleetManager.Business.ViewModels.MaintenanceViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace FleetManager.App.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ManageMaintenanceController : Controller
    {
        private readonly IMaintenanceService _service;
        private readonly IAuthUser _auth;
        private readonly ILogger<ManageMaintenanceController> _logger;
        private readonly IDriverVehicleService _assignmentService;
        private readonly IAdminVehicleService _vehicleService;
        private readonly IManageDriverService _driverService;

        public ManageMaintenanceController(
            IMaintenanceService svc,
            IAuthUser auth,
            ILogger<ManageMaintenanceController> logger,
            IDriverVehicleService assignmentService,
            IManageDriverService driverService,
            IAdminVehicleService vehicleService)
        {
            _service = svc;
            _auth = auth;
            _logger = logger;
            _assignmentService = assignmentService;
            _driverService = driverService;
            _vehicleService = vehicleService;
        }

        // ─── TICKETS ─────────────────────────────────────────────────────────────

        /// <summary>
        /// List all tickets for current branch, paginated.
        /// </summary>
        public async Task<IActionResult> Index(
            string CurrentFilter = "ByBranch",
            long? DriverId = null,
            long? VehicleId = null,
            int page = 1,
            int pageSize = 20)
        {
            try
            {
                // 1) run the appropriate service call
                var resp = CurrentFilter switch
                {
                    "ByDriver" => await _service.QueryTicketsByDriverAsync(page, pageSize, DriverId),
                    "ByVehicle" => await _service.QueryTicketsByVehicleAsync(page, pageSize, VehicleId ?? 0),
                    _ => await _service.QueryTicketsByBranchAsync(page, pageSize, _auth.CompanyBranchId)
                };

                if (!resp.Success)
                {
                    TempData["ErrorMessage"] = resp.Message;
                    return View("Error");
                }

                var ticketsPage = resp.Result;

                // 2) load dropdowns
                var vehicles = await _vehicleService.QueryVehicles(new VehicleFilterDto { BranchId = _auth.CompanyBranchId })
                    .OrderBy(v => v.PlateNo)
                    .Select(v => new SelectListItem($"{v.PlateNo} — {v.Make} {v.Model}", v.Id.ToString()))
                    .ToListAsync();

                var drivers = await _driverService.QueryDriversForBranch(_auth.CompanyBranchId)
                    .OrderBy(d => d.FullName)
                    .Select(d => new SelectListItem(d.FullName, d.Id.ToString()))
                    .ToListAsync();

                // 3) push everything into the VM
                var vm = new MaintenanceTicketListViewModel
                {
                    Tickets = ticketsPage.Items,
                    Pagination = new PaginationDto
                    {
                        CurrentPage = page,
                        PageSize = pageSize,
                        TotalItems = ticketsPage.TotalPages
                    },
                    CurrentFilter = CurrentFilter,
                    DriverId = DriverId,
                    VehicleId = VehicleId,
                    Drivers = drivers,
                    Vehicles = vehicles,
                    EditModel = new MaintenanceTicketStatusEditViewModel() // empty shell
                };

                return View(vm);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading maintenance tickets");
                return View("Error", new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
            }
        }


        /// <summary>
        /// Show the create‐ticket form.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            try
            {
                // load dropdowns: Drivers, Vehicles, PartCategories

                var driverUserId = _auth.UserId!;
                var driverId = await _assignmentService.GetDriverIdByUserAsync(driverUserId);

                // Vehicles assigned to this driver:
                var vehicles = await _assignmentService
                    .QueryAssignmentsByDriver(driverId)
                    .Select(a => new SelectListItem(a.VehicleMakeModel, a.VehicleId.ToString()))
                    .ToListAsync();
                var priorities = _service.GetPriorityTypeOptions();

                // All part categories:
                var categories = await _service.GetPartCategoriesAsync();

                var vm = new MaintenanceTicketCreateViewModel
                {
                    DriverId = driverId,
                    Vehicles = vehicles,
                    PartCategories = categories,
                    Priorities = priorities,
                    Items = new List<MaintenanceTicketItemInputViewModel>
                    {
                        new MaintenanceTicketItemInputViewModel()  // <-- one empty row
                    }
                };
                return View(vm);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        /// <summary>
        /// POST: create a new maintenance ticket.
        /// </summary>
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MaintenanceTicketCreateViewModel vm)
        {
            try
            {
                // repost dropdowns on validation failure
                var driverUserId = _auth.UserId!;
                var driverId = await _assignmentService.GetDriverIdByUserAsync(driverUserId);

                var vehicles = await _assignmentService
                    .QueryAssignmentsByDriver(driverId)
                    .Select(a => new SelectListItem(a.VehicleMakeModel, a.VehicleId.ToString()))
                    .ToListAsync();
                vm.Priorities = _service.GetPriorityTypeOptions();

                vm.PartCategories = await _service.GetPartCategoriesAsync();
                if (!ModelState.IsValid)
                    return View(vm);

                var input = new MaintenanceTicketInputDto
                {
                    DriverId = driverId,
                    VehicleId = vm.VehicleId,
                    Subject = vm.Subject,
                    Notes = vm.Notes,
                    Priority = vm.Priority,
                    Items = vm.Items.Select(i => new MaintenanceTicketItemInputDto
                    {
                        PartCategoryId = i.PartCategoryId,
                        PartId = i.PartId,
                        CustomDescription = i.CustomDescription,
                        Quantity = i.Quantity,
                        UnitPrice = i.UnitPrice
                    }).ToList()
                };

                var resp = await _service.CreateTicketAsync(input, _auth.UserId);
                if (!resp.Success)
                {
                    ModelState.AddModelError("", resp.Message);
                    return View(vm);
                }

                TempData["SuccessMessage"] = "Ticket created successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating maintenance ticket");
                ModelState.AddModelError("", "Unexpected error.");
                return View(vm);
            }
        }

        /// <summary>
        /// Details of a single ticket.
        /// </summary>
        public async Task<IActionResult> Details(long id)
        {
            try
            {
                var ticket = await _service.GetTicketByIdAsync(id);
                if (ticket == null)
                    return NotFound();

                return View(ticket);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading ticket details #{TicketId}", id);
                return View("Error", new ErrorViewModel
                {
                    RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
                });
            }
        }



        //[HttpPost, ValidateAntiForgeryToken]
        //public async Task<IActionResult> UpdateTicketStatus(MaintenanceTicketStatusEditViewModel vm)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        var errors = ModelState
        //            .Where(kvp => kvp.Value.Errors.Any())
        //            .ToDictionary(
        //               kvp => kvp.Key,
        //               kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
        //            );
        //        return Json(new { success = false, message = "Validation failed", errors });
        //    }

        //    var input = new UpdateTicketStatusDto
        //    {
        //        TicketId = vm.TicketId,
        //        NewStatus = vm.NewStatus,
        //        InvoiceStatus = vm.NewInvoiceStatus,
        //        AdminNotes = vm.AdminNotes
        //    };

        //    var resp = await _service.UpdateTicketStatusAsync(input);
        //    if (!resp.Success)
        //        return Json(new { success = false, message = resp.Message });

        //    // return ticketId & newStatus so client can update inline
        //    return Json(new
        //    {
        //        success = true,
        //        ticketId = vm.TicketId,
        //        newStatus = vm.NewStatus.ToString(),
        //        newInvoiceStatus = vm.NewInvoiceStatus
        //    });
        //}


        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateTicketStatus(MaintenanceTicketStatusEditViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(kvp => kvp.Value.Errors.Any())
                    .ToDictionary(
                       kvp => kvp.Key,
                       kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                    );
                return Json(new { success = false, message = "Validation failed", errors });
            }

            var input = new UpdateTicketStatusDto
            {
                TicketId = vm.TicketId,
                NewStatus = vm.NewStatus,
                InvoiceStatus = vm.NewInvoiceStatus,
                AdminNotes = vm.AdminNotes
            };

            var resp = await _service.UpdateTicketStatusAsync(input);
            if (!resp.Success)
                return Json(new { success = false, message = resp.Message });

            // Return comprehensive status information for dynamic UI updates
            var response = new
            {
                success = true,
                ticketId = vm.TicketId,
                newStatus = vm.NewStatus.ToString(),
                newInvoiceStatus = vm.NewInvoiceStatus?.ToString(),
                message = "Status updated successfully",
                // Optional: Include timestamp for audit trail
                updatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };

            return Json(response);
        }




        // ─── INVOICES ────────────────────────────────────────────────────────────

        /// <summary>
        /// List all invoices for current branch, paginated.
        /// </summary>
        public async Task<IActionResult> Invoices(int page = 1, int pageSize = 20)
        {
            try
            {
                var driverId = await _assignmentService.GetDriverIdByUserAsync(_auth.UserId!);
                var resp = await _service.QueryInvoicesByDriverAsync(page, pageSize, driverId);
                if (!resp.Success)
                {
                    TempData["ErrorMessage"] = resp.Message;
                    return View("Error");
                }
                var paged = resp.Result;

                var vm = new InvoiceListViewModel
                {
                    Invoices = resp.Result.Items,
                    Pagination = new PaginationDto
                    {
                        CurrentPage = paged.Page,
                        PageSize = paged.PageSize,
                        TotalItems = paged.TotalPages
                    }
                };
                return View(vm);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading invoices");
                return View("Error");
            }
        }

        /// <summary>
        /// Invoice details.
        /// </summary>
        public async Task<IActionResult> InvoiceDetails(long id)
        {
            try
            {
                var inv = await _service.GetInvoiceByIdAsync(id);
                if (inv == null) return NotFound();
                return View(inv);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading invoice #{InvoiceId}", id);
                return View("Error");
            }
        }

        /// <summary>
        /// POST: change invoice status (e.g. mark paid).
        /// </summary>
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateInvoiceStatus(long invoiceId, InvoiceStatus newStatus)
        {
            try
            {
                var resp = await _service.UpdateInvoiceStatusAsync(invoiceId, newStatus);
                TempData[resp.Success ? "SuccessMessage" : "ErrorMessage"] = resp.Message;
                return RedirectToAction(nameof(InvoiceDetails), new { id = invoiceId });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating invoice status #{InvoiceId}", invoiceId);
                TempData["ErrorMessage"] = "Unexpected error.";
                return RedirectToAction(nameof(InvoiceDetails), new { id = invoiceId });
            }
        }


        [HttpGet]
        public async Task<IActionResult> GetPartsByCategory(int categoryId)
        {
            // Service returns IEnumerable<SelectListItem>
            var models = await _service.GetPartsByCategoryAsync(categoryId);

            // Return JSON in the form [{ value: "...", text: "..." }, …]
            return Json(models);
        }
    }

}
