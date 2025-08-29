using FleetManager.Business.DataObjects.AdminDashboardDto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.CompanyOwnerDashboardDto
{
    public class BranchDetailDto
    {
        public long BranchId { get; set; }
        public string? BranchName { get; set; }
        public string? ManagerName { get; set; }
        public BranchSummaryDto Summary { get; set; } = new BranchSummaryDto();
        public List<RecentFuelDto> RecentFuelLogs { get; set; } = new List<RecentFuelDto>();
        public List<RecentTicketDto> RecentMaintenanceTickets { get; set; } = new List<RecentTicketDto>();
        public List<MonthPointDto> ExpensesByMonth { get; set; } = new List<MonthPointDto>();
        public List<TopVehicleDto> TopVehiclesByFuel { get; set; } = new List<TopVehicleDto>();
    }
}
