using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.ReportsCenter
{
    public class TripSummaryDto
    {
        public long TripId { get; set; }
        public string TripNumber { get; set; } = "";
        public string DriverName { get; set; } = "";
        public string VehiclePlate { get; set; } = "";
        public string Origin { get; set; } = "";
        public string Destination { get; set; } = "";
        public DateTime ScheduledStart { get; set; }
        public DateTime? ActualStart { get; set; }
        public DateTime? ActualEnd { get; set; }
        public decimal? EstimatedDistance { get; set; }
        public decimal? ActualDistance { get; set; }
        public decimal? EstimatedFuelCost { get; set; }
        public decimal? ActualFuelCost { get; set; }
        public double DurationMinutes => (ActualStart.HasValue && ActualEnd.HasValue) ? (ActualEnd.Value - ActualStart.Value).TotalMinutes : 0;
    }
}
