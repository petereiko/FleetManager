using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.ReportsCenter
{
    public class FuelConsumptionReportDto
    {
        public decimal TotalVolume { get; set; }
        public decimal TotalCost { get; set; }
        public decimal AveragePricePerUnit { get; set; }
        public decimal CostPerKm { get; set; }
        public List<FuelByVehicleDto> ByVehicle { get; set; } = new();
    }
}
