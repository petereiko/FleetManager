using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.ReportsCenter
{
    public class ReportFilter
    {
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public long? VehicleId { get; set; }
        public long? DriverId { get; set; }
        public string? Route { get; set; }
        public long? CompanyBranchId { get; set; } // optional override (usually null; _authUser enforced)
    }
}
