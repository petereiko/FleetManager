using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.AdminDashboardDto
{
    public class DashboardRequestDto
    {
        public long? CompanyId { get; set; }
        public long? CompanyBranchId { get; set; }
        public DateTimeOffset? DateFrom { get; set; }
        public DateTimeOffset? DateTo { get; set; }
        public string TimeZoneId { get; set; } = "UTC";
        public int RecentListSize { get; set; } = 5;
    }
}
