using FleetManager.Business.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.ReportsDto
{
    public class FuelConsumptionReportDto
    {
        public long VehicleId { get; set; }
        public string VehicleIdentifier { get; set; }
        public string DriverName { get; set; }
        public DateTime Date { get; set; }
        public decimal Volume { get; set; }
        public decimal Cost { get; set; }
        public int? Odometer { get; set; }
        public int? PreviousOdometer { get; set; }
        public decimal PricePerLiter { get; set; }
        public decimal? ConsumptionRate { get; set; }
        public FuelType FuelType { get; set; }
        public string Notes { get; set; }
    }

}
