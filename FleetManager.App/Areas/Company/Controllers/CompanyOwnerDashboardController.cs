using FleetManager.Business.DataObjects.AdminDashboardDto;
using FleetManager.Business.Interfaces.CompanyDashboardModule;
using FleetManager.Business.Interfaces.UserModule;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FleetManager.App.Areas.Company.Controllers
{
    //[Authorize(Roles = "CompanyOwner")]
    [ApiController]
    [Route("api/company-owner")]
    public class CompanyOwnerDashboardController : ControllerBase
    {
        private readonly ICompanyOwnerDashboardService _svc;
        private readonly IAuthUser _authUser;

        public CompanyOwnerDashboardController(ICompanyOwnerDashboardService svc, IAuthUser authUser)
        {
            _svc = svc;

            _authUser = authUser;
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard([FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int recentSize = 5, CancellationToken ct = default)
        {
            var companyId = _authUser.CompanyId;
            var req = new DashboardRequestDto { CompanyId = companyId, DateFrom = from, DateTo = to, RecentListSize = recentSize };
            var dto = await _svc.GetCompanyOwnerDashboardAsync(req, ct);
            return Ok(dto);
        }

        [HttpGet("branch/{branchId}")]
        public async Task<IActionResult> GetBranchDetails(long branchId, [FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct = default)
        {
            var companyId = _authUser.CompanyId;
            var req = new DashboardRequestDto { CompanyId = companyId, DateFrom = from, DateTo = to, RecentListSize = 10, CompanyBranchId = branchId };
            var detail = await _svc.GetBranchDetailsAsync(branchId, req, ct);
            return Ok(detail);
        }

        [HttpGet("expenses-by-month")]
        public async Task<IActionResult> GetExpensesByMonth([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct = default)
        {
            var companyId = _authUser.CompanyId;
            var req = new DashboardRequestDto { CompanyId = companyId, DateFrom = from, DateTo = to };
            var list = await _svc.GetCompanyExpensesByMonthAsync(req, ct);
            return Ok(list);
        }
    }

}
