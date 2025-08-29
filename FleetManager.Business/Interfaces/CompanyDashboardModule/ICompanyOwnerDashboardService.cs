using FleetManager.Business.DataObjects.AdminDashboardDto;
using FleetManager.Business.DataObjects.CompanyOwnerDashboardDto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Interfaces.CompanyDashboardModule
{
    public interface ICompanyOwnerDashboardService
    {
        Task<CompanyOwnerDashboardDto> GetCompanyOwnerDashboardAsync(DashboardRequestDto req, CancellationToken ct = default);
        Task<BranchDetailDto> GetBranchDetailsAsync(long branchId, DashboardRequestDto req, CancellationToken ct = default);
        Task<List<MonthPointDto>> GetCompanyExpensesByMonthAsync(DashboardRequestDto req, CancellationToken ct = default);
        
    }

}
