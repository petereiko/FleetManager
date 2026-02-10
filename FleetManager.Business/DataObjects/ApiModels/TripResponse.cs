using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.ApiModels
{
    public class TripResponse
    {
        public long TripId { get; set; }
        public string TripNumber { get; set; } = string.Empty;

        // Vehicle Info
        public long VehicleId { get; set; }
        public string VehiclePlateNo { get; set; } = string.Empty;
        public string VehicleMakeModel { get; set; } = string.Empty;
        public int? VehicleMileage { get; set; }

        // Trip Details
        public string Origin { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public string Purpose { get; set; } = string.Empty;
        public string? Description { get; set; }

        // Scheduling
        public DateTime ScheduledStartDate { get; set; }
        public DateTime ScheduledEndDate { get; set; }
        public DateTime? ActualStartDate { get; set; }
        public DateTime? ActualEndDate { get; set; }

        // Metrics
        public decimal? EstimatedDistance { get; set; }
        public decimal? ActualDistance { get; set; }
        public decimal? EstimatedFuelCost { get; set; }
        public decimal? ActualFuelCost { get; set; }

        // Odometer
        public int? StartOdometer { get; set; }
        public int? EndOdometer { get; set; }

        // Status
        public string Status { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;

        // Additional
        public string? Notes { get; set; }
        public bool RequiresApproval { get; set; }
        public bool HasSuspiciousLocation { get; set; }
    }
}
