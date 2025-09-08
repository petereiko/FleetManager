using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.CompanyOwnerDashboardDto
{
    public class BranchSummaryDto
    {
        public long BranchId { get; set; }
        public string? BranchName { get; set; }
        public string? ManagerName { get; set; }

        public int TotalDrivers { get; set; }
        public int ActiveDrivers { get; set; }

        public int TotalVehicles { get; set; }
        public int ActiveVehicles { get; set; }
        public int AssignedVehicleCount { get; set; }

        public int OpenMaintenanceTickets { get; set; }
        public int OverdueMaintenanceTickets { get; set; }

        public int VendorsCount { get; set; }

        public decimal FuelSpend { get; set; }
        public decimal MaintenanceSpend { get; set; }
        public decimal FinesSpend { get; set; }
        public decimal TotalSpend { get; set; }

        public string? CompanyAdminName { get; set; }
        // NEW: Performance percentage for progress bars
        public double PerformancePercentage { get; set; }
    }


    public class BranchListItemDto 
    { 
        public long BranchId { get; set; } 
        public string BranchName { get; set; } 
    }

}
