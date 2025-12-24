using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.ReportsCenter
{
    public class CostAnalysisDto
    {
        public decimal FuelCost { get; set; }
        public decimal MaintenanceCost { get; set; }
        public decimal TollsAndFines { get; set; }
        public decimal OtherCosts { get; set; }
        public decimal TotalCost => FuelCost + MaintenanceCost + TollsAndFines + OtherCosts;
        public List<CostByVehicleDto> ByVehicle { get; set; } = new();
    }
}
