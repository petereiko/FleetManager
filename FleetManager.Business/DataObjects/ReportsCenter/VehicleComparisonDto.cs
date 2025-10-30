using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.ReportsCenter
{
    public class VehicleComparisonDto
    {
        public long VehicleId { get; set; }
        public string PlateNo { get; set; } = "";
        public decimal TotalDistance { get; set; }
        public decimal FuelCost { get; set; }
        public decimal MaintenanceCost { get; set; }
        public decimal CostPerKm => TotalDistance == 0 ? 0 : (FuelCost + MaintenanceCost) / TotalDistance;
    }
}
