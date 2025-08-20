using FleetManager.Business.Database.IdentityModels;
using FleetManager.Business.DataObjects.AdminDashboardDto;
using FleetManager.Business.Interfaces.AdminDashboardModule;
using FleetManager.Business.Interfaces.UserModule;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

namespace FleetManager.App.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    [Route("admin/[controller]")]
    public class DashboardController : Controller
    {

        private readonly IAdminDashboardService _dashboardService;
        private readonly IAuthUser _authUser;

        public DashboardController(IAdminDashboardService dashboardService, IAuthUser authUser)
        {
            _dashboardService = dashboardService;
            _authUser = authUser;
        }

        //public IActionResult Index()
        //{
        //    return View();
        //}



        // Main view: loads counts, money summary and small previews
        [HttpGet("index")]
        public async Task<IActionResult> Index(CancellationToken ct)
        {
            var branchId = _authUser.CompanyBranchId;

            var req = new DashboardRequestDto
            {
                CompanyBranchId = branchId,
                // null DateFrom/DateTo => default last 6 months
                RecentListSize = 5
            };

            var dto = await _dashboardService.GetAdminDashboardAsync(req, ct);
            return View("Index", dto); // Views/Admin/AdminDashboard/Dashboard.cshtml
        }

        // --- JSON endpoints used by the UI (AJAX) ---

        [HttpGet("fuel-by-month")]
        public async Task<IActionResult> FuelByMonth([FromQuery] string? fromDate, [FromQuery] string? toDate, CancellationToken ct)
        {
            var branchId = _authUser.CompanyBranchId;
            var req = new DashboardRequestDto
            {
                CompanyBranchId = branchId,
                DateFrom = ParseDateOrNull(fromDate),
                DateTo = ParseDateOrNull(toDate),
                RecentListSize = 0
            };
            var list = await _dashboardService.GetFuelByMonthAsync(req, ct);
            return Ok(list);
        }

        [HttpGet("maintenance-by-month")]
        public async Task<IActionResult> MaintenanceByMonth([FromQuery] string? fromDate, [FromQuery] string? toDate, CancellationToken ct)
        {
            var branchId = _authUser.CompanyBranchId;
            var req = new DashboardRequestDto
            {
                CompanyBranchId = branchId,
                DateFrom = ParseDateOrNull(fromDate),
                DateTo = ParseDateOrNull(toDate)
            };
            var list = await _dashboardService.GetMaintenanceByMonthAsync(req, ct);
            return Ok(list);
        }

        [HttpGet("tickets-by-status")]
        public async Task<IActionResult> TicketsByStatus([FromQuery] string? fromDate, [FromQuery] string? toDate, CancellationToken ct)
        {
            var branchId = _authUser.CompanyBranchId;
            var req = new DashboardRequestDto
            {
                CompanyBranchId = branchId,
                DateFrom = ParseDateOrNull(fromDate),
                DateTo = ParseDateOrNull(toDate)
            };
            var list = await _dashboardService.GetTicketsByStatusAsync(req, ct);
            return Ok(list);
        }

        [HttpGet("top-vehicles-by-fuel")]
        public async Task<IActionResult> TopVehiclesByFuel([FromQuery] int top = 10, [FromQuery] string? fromDate = null, [FromQuery] string? toDate = null, CancellationToken ct = default)
        {
            var branchId = _authUser.CompanyBranchId;
            var req = new DashboardRequestDto
            {
                CompanyBranchId = branchId,
                DateFrom = ParseDateOrNull(fromDate),
                DateTo = ParseDateOrNull(toDate)
            };
            var list = await _dashboardService.GetTopVehiclesByFuelAsync(req, top, ct);
            return Ok(list);
        }

        [HttpGet("maintenance-cost-by-part-category")]
        public async Task<IActionResult> MaintenanceCostByPartCategory([FromQuery] int top = 10, [FromQuery] string? fromDate = null, [FromQuery] string? toDate = null, CancellationToken ct = default)
        {
            var branchId = _authUser.CompanyBranchId;
            var req = new DashboardRequestDto
            {
                CompanyBranchId = branchId,
                DateFrom = ParseDateOrNull(fromDate),
                DateTo = ParseDateOrNull(toDate)
            };
            var list = await _dashboardService.GetMaintenanceCostByPartCategoryAsync(req, top, ct);
            return Ok(list);
        }

        [HttpGet("recent-maintenance-tickets")]
        public async Task<IActionResult> RecentMaintenanceTickets([FromQuery] int size = 5, CancellationToken ct = default)
        {
            var branchId = _authUser.CompanyBranchId;
            var req = new DashboardRequestDto
            {
                CompanyBranchId = branchId,
                RecentListSize = size
            };
            var list = await _dashboardService.GetRecentMaintenanceTicketsAsync(req, ct);
            return Ok(list);
        }

        [HttpGet("recent-fuel-logs")]
        public async Task<IActionResult> RecentFuelLogs([FromQuery] int size = 5, CancellationToken ct = default)
        {
            var branchId = _authUser.CompanyBranchId;
            var req = new DashboardRequestDto
            {
                CompanyBranchId = branchId,
                RecentListSize = size
            };
            var list = await _dashboardService.GetRecentFuelLogsAsync(req, ct);
            return Ok(list);
        }

        [HttpGet("recent-contacts")]
        public async Task<IActionResult> RecentContacts([FromQuery] int size = 5, CancellationToken ct = default)
        {
            var branchId = _authUser.CompanyBranchId;
            var req = new DashboardRequestDto
            {
                CompanyBranchId = branchId,
                RecentListSize = size
            };
            var list = await _dashboardService.GetRecentContactsAsync(req, ct);
            return Ok(list);
        }

        // --- small helpers ---
        private static DateTimeOffset? ParseDateOrNull(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            if (DateTimeOffset.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dto))
                return dto;
            return null;
        }
    }
}
