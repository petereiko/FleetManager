using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.CompanyOwnerDashboardDto
{
    public class CompanyOwnerDashboardDto
    {
        public long CompanyId { get; set; }
        public string? CompanyName { get; set; }
        public DateTimeOffset GeneratedAt { get; set; }
        public string TimeZoneId { get; set; } = "UTC";
        public bool CacheHit { get; set; }
        public int BranchCount { get; set; }
        public int AdminCount { get; set; }
        public int VehicleCount { get; set; }
        public int DriverCount { get; set; }
        public TotalsDto Totals { get; set; } = new TotalsDto();
        public List<BranchSummaryDto> Branches { get; set; } = new List<BranchSummaryDto>();

        // NEW: Vehicle status distribution for pie chart
        public Dictionary<string, int> VehicleStatusDistribution { get; set; } = new Dictionary<string, int>();

        public List<BranchListItemDto> AllBranches { get; set; } = new();
        public bool IsFiltered { get; set; } 

    }
}
