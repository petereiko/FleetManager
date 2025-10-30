using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.ReportsCenter
{
    public class CostByVehicleDto
    {
        public long VehicleId { get; set; }
        public string PlateNo { get; set; } = "";
        public decimal FuelCost { get; set; }
        public decimal MaintenanceCost { get; set; }
        public decimal TollsAndFines { get; set; }
        public decimal TotalCost => FuelCost + MaintenanceCost + TollsAndFines;
    }
}
