using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.ReportsCenter
{
    public class VehicleActivityDto
    {
        public long VehicleId { get; set; }
        public string PlateNo { get; set; } = "";
        public string VehicleMake { get; set; } = "";
        public string VehicleModel { get; set; } = "";
        public decimal DistanceKm { get; set; }
        public int TripsCount { get; set; }
        public string Status { get; set; } = ""; // e.g. Active/Idle/UnderMaintenance
    }
}
