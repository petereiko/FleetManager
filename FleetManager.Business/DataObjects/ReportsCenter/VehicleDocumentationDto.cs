using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.ReportsCenter
{
    public class VehicleDocumentationDto
    {
        public long VehicleId { get; set; }
        public string PlateNo { get; set; } = "";
        public DateTime? InsuranceExpiryDate { get; set; }
        public DateTime? RoadWorthyExpiryDate { get; set; }
        public bool InsuranceExpired => InsuranceExpiryDate.HasValue && InsuranceExpiryDate.Value < DateTime.UtcNow;
        public bool RoadWorthyExpired => RoadWorthyExpiryDate.HasValue && RoadWorthyExpiryDate.Value < DateTime.UtcNow;
    }
}
