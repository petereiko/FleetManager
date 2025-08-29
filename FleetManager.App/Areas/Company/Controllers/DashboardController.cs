
using FleetManager.Business.Database.IdentityModels;
using FleetManager.Business.DataObjects.AdminDashboardDto;
using FleetManager.Business.Interfaces.CompanyDashboardModule;
using FleetManager.Business.Interfaces.UserModule;
using FleetManager.Business.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FleetManager.App.Areas.Company.Controllers
{
    [Area("Company")]
    //[Authorize(Roles = "CompanyOwner")]
    public class DashboardController : Controller
    {
        private readonly ICompanyOwnerDashboardService _dashboardSvc;
        private readonly ILogger<DashboardController> _logger;
        private readonly IAuthUser _authUser;

        public DashboardController(
            ICompanyOwnerDashboardService dashboardSvc,
            ILogger<DashboardController> logger,
            IAuthUser authUser)
        {
            _dashboardSvc = dashboardSvc;
            _logger = logger;
            _authUser = authUser;
        }

        

        /// <summary>
        /// Company owner dashboard (hybrid server-side). Optional query params: from, to, recentSize.
        /// Renders the Index view with a CompanyOwnerDashboardDto model.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index(DateTime? from = null, DateTime? to = null, int recentSize = 5, CancellationToken ct = default)
        {
            try
            {
                var companyId = _authUser.CompanyId;

                var req = new DashboardRequestDto
                {
                    CompanyId = companyId,
                    DateFrom = from,
                    DateTo = to,
                    RecentListSize = Math.Max(1, recentSize)
                };

                var model = await _dashboardSvc.GetCompanyOwnerDashboardAsync(req, ct);

                // Pass model to strongly-typed Razor view: @model CompanyOwnerDashboardDto
                return View(model);
            }
            catch (UnauthorizedAccessException uae)
            {
                _logger.LogWarning(uae, "Unauthorized access when building company owner dashboard.");
                return Forbid();
            }
            catch (InvalidOperationException ioe)
            {
                // e.g. user not assigned to a company
                _logger.LogWarning(ioe, "Invalid operation when building dashboard.");
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while building company owner dashboard.");
                // Return 500. Optionally return a friendly Error view instead.
                return StatusCode(500);
            }
        }

        [HttpGet]
        public async Task<IActionResult> BranchDetails(long branchId, DateTime? from = null, DateTime? to = null, int recentSize = 10, CancellationToken ct = default)
        {
            try
            {
                var companyId = _authUser.CompanyId;

                // Build request and validate branch belongs to company
                var req = new DashboardRequestDto
                {
                    CompanyId = companyId,
                    CompanyBranchId = branchId,
                    DateFrom = from,
                    DateTo = to,
                    RecentListSize = Math.Max(1, recentSize)
                };

                // This will throw KeyNotFoundException if branch doesn't exist (service checks)
                var detail = await _dashboardSvc.GetBranchDetailsAsync(branchId, req, ct);

                // Option A: return JSON for client-side modal (recommended if your JS expects JSON)
                return Json(detail);

                // Option B (server-side partial): uncomment to return a PartialView that the modal can load
                // return PartialView("_BranchDetailPartial", detail);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading branch details for branchId {BranchId}", branchId);
                return StatusCode(500);
            }
        }

        [HttpGet]
        public async Task<IActionResult> ExpensesByMonth(DateTime? from = null, DateTime? to = null, CancellationToken ct = default)
        {
            try
            {
                var companyId = _authUser.CompanyId;

                var req = new DashboardRequestDto
                {
                    CompanyId = companyId,
                    DateFrom = from,
                    DateTo = to
                };

                var list = await _dashboardSvc.GetCompanyExpensesByMonthAsync(req, ct);

                // Return JSON that the Chart.js script can consume
                return Json(list);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading expenses by month");
                return StatusCode(500);
            }
        }

        [HttpGet]
        public async Task<IActionResult> RecentActivities(long? branchId = null, int size = 10, DateTime? from = null, DateTime? to = null, CancellationToken ct = default)
        {
            // Convenience endpoint returning combined recent items for the recent activities table.
            // It uses admin service methods via service (we'll call GetBranchDetailsAsync for branch-specific lists)
            try
            {
                var companyId = _authUser.CompanyId;
                DashboardRequestDto req = new DashboardRequestDto
                {
                    CompanyId = companyId,
                    CompanyBranchId = branchId,
                    DateFrom = from,
                    DateTo = to,
                    RecentListSize = Math.Max(1, size)
                };

                if (branchId.HasValue)
                {
                    var detail = await _dashboardSvc.GetBranchDetailsAsync(branchId.Value, req, ct);
                    // return the combined recent entries (fuel + tickets) as JSON
                    return Json(new
                    {
                        recentFuel = detail.RecentFuelLogs ?? new List<RecentFuelDto>(),
                        recentTickets = detail.RecentMaintenanceTickets ?? new List<RecentTicketDto>()
                    });
                }
                else
                {
                    // Company-wide quick aggregation: you can return the first branch's recent items (or merge across branches)
                    var dash = await _dashboardSvc.GetCompanyOwnerDashboardAsync(req, ct);
                    var firstBranch = dash.Branches.FirstOrDefault();
                    if (firstBranch == null) return Json(new { recentFuel = new object[0], recentTickets = new object[0] });

                    var detail = await _dashboardSvc.GetBranchDetailsAsync(firstBranch.BranchId, req, ct);
                    return Json(new
                    {
                        recentFuel = detail.RecentFuelLogs ?? new List<RecentFuelDto>(),
                        recentTickets = detail.RecentMaintenanceTickets ?? new List<RecentTicketDto>()
                    });
                }
            }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (Exception ex) { _logger.LogError(ex, "Error loading recent activities"); return StatusCode(500); }
        }


    }
}
