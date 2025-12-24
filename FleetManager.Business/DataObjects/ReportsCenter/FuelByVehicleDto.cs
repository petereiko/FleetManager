using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.ReportsCenter
{
    public class FuelByVehicleDto
    {
        public long VehicleId { get; set; }
        public string PlateNo { get; set; } = "";
        public decimal Volume { get; set; }
        public decimal Cost { get; set; }
        public decimal DistanceKm { get; set; }
        public decimal CostPerKm => DistanceKm <= 0 ? 0 : Cost / DistanceKm;
    }
}
