using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.ReportsDto
{
    public class FuelEfficiencyReportDto
    {
        public long VehicleId { get; set; }
        public string VehicleIdentifier { get; set; }
        public string Make { get; set; }
        public string Model { get; set; }
        public decimal AverageConsumption { get; set; }
        public decimal TotalFuelCost { get; set; }
        public decimal TotalVolume { get; set; }
        public decimal TotalDistance { get; set; }
        public decimal CostPerKm { get; set; }
        public decimal EfficiencyRating { get; set; }
        public int TotalFillUps { get; set; }
        public DateTime FirstFillUp { get; set; }
        public DateTime LastFillUp { get; set; }
    }
}
