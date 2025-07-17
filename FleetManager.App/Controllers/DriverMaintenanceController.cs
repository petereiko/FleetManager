using FleetManager.Business.DataObjects.MaintenanceDto;
using FleetManager.Business.DataObjects.VehicleDto;
using FleetManager.Business.Enums;
using FleetManager.Business.Interfaces.DriverVehicleModule;
using FleetManager.Business.Interfaces.MaintenanceModule;
using FleetManager.Business.Interfaces.UserModule;
using FleetManager.Business.ViewModels.MaintenanceViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FleetManager.App.Controllers
{
    public class DriverMaintenanceController : Controller
    {
        private readonly IMaintenanceService _service;
        private readonly IAuthUser _auth;
        private readonly ILogger<DriverMaintenanceController> _logger;
        private readonly IDriverVehicleService _assignmentService;

        public DriverMaintenanceController(
            IMaintenanceService svc,
            IAuthUser auth,
            ILogger<DriverMaintenanceController> logger,
            IDriverVehicleService assignmentService)
        {
            _service = svc;
            _auth = auth;
            _logger = logger;
            _assignmentService = assignmentService;
        }

        // ─── TICKETS ─────────────────────────────────────────────────────────────

        /// <summary>
        /// List all tickets for current branch, paginated.
        /// </summary>
        public async Task<IActionResult> Index(int page = 1, int pageSize = 20)
        {
            try
            {
                var driverId = await _assignmentService.GetDriverIdByUserAsync(_auth.UserId!);
                var resp = await _service.QueryTicketsByDriverAsync(page, pageSize, driverId);
                if (!resp.Success)
                {
                    TempData["ErrorMessage"] = resp.Message;
                    return View("Error");
                }
                var paged = resp.Result;

                var vm = new MaintenanceTicketListViewModel
                {
                    Tickets = paged.Items,
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
                _logger.LogError(ex, "Error loading maintenance tickets");
                return View("Error");
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

                // All part categories:
                var categories = await _service.GetPartCategoriesAsync();

                var vm = new MaintenanceTicketCreateViewModel
                {
                    DriverId = driverId,
                    Vehicles = vehicles,
                    PartCategories = categories,
                    Items = new List<MaintenanceTicketItemInputViewModel>() // start empty
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

                vm.PartCategories = await _service.GetPartCategoriesAsync();

                if (!ModelState.IsValid)
                    return View(vm);

                var input = new MaintenanceTicketInputDto
                {
                    DriverId = driverId,
                    VehicleId = vm.VehicleId,
                    Subject = vm.Subject,
                    Notes = vm.Notes,
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
                return View("Error");
            }
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
