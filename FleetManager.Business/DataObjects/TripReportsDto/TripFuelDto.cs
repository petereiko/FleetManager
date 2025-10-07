using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.TripReportsDto
{
    public class TripFuelDto
    {
        public long TripId { get; set; }
        public string TripNumber { get; set; } = "";
        public long? DriverId { get; set; }
        public string? DriverName { get; set; }
        public long? VehicleId { get; set; }
        public string? VehiclePlateNo { get; set; }
        public decimal? ActualDistance { get; set; }
        public decimal? ActualFuelCost { get; set; }
        public decimal? FuelPerKm => (ActualDistance.HasValue && ActualDistance.Value > 0 && ActualFuelCost.HasValue) ? ActualFuelCost.Value / ActualDistance.Value : (decimal?)null;
        public DateTime CreatedDate { get; set; }
    }
}
