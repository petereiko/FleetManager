using FleetManager.Business.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.TripsDto
{
    public class TripDto
    {
        public long Id { get; set; }
        public string TripNumber { get; set; }
        public long CompanyBranchId { get; set; }
        public long CompanyId { get; set; }

        // Vehicle Info
        public long VehicleId { get; set; }
        public string VehiclePlateNo { get; set; }
        public string VehicleMake { get; set; }
        public string VehicleModel { get; set; }
        public int? VehicleMileage { get; set; }

        // Driver Info
        public long? DriverId { get; set; }
        public string? DriverName { get; set; }
        public string? DriverLicenseNumber { get; set; }

        // Trip Details
        public string Origin { get; set; }
        public string Destination { get; set; }
        public string Purpose { get; set; }
        public string? Description { get; set; }

        // Scheduling
        public DateTime ScheduledStartDate { get; set; }
        public DateTime ScheduledEndDate { get; set; }
        public DateTime? ActualStartDate { get; set; }
        public DateTime? ActualEndDate { get; set; }
        public DateTime? StartTrip {get; set;}
        public DateTime? EndTrip {get; set; }

        // Metrics
        public decimal? EstimatedDistance { get; set; }
        public decimal? ActualDistance { get; set; }
        public decimal? EstimatedFuelCost { get; set; }
        public decimal? ActualFuelCost { get; set; }

        // Odometer
        public int? StartOdometer { get; set; }
        public int? EndOdometer { get; set; }

        // Status
        public TripStatus Status { get; set; }
        public string StatusDisplay { get; set; }
        public TripPriority Priority { get; set; }
        public string PriorityDisplay { get; set; }

        // Assignment
        public string? AssignedBy { get; set; }
        public DateTime? AssignedDate { get; set; }

        // Approval
        public bool RequiresApproval { get; set; }
        public bool? IsApproved { get; set; }
        public string? ApprovedBy { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public string? RejectionReason { get; set; }

        // Additional
        public string? Notes { get; set; }
        public string? CancellationReason { get; set; }
        public DateTime? CancellationDate { get; set; }
        public bool HasSuspiciousLocation { get; set; }

        // Base Entity
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public string? CreatedBy { get; set; }
        public string? ModifiedBy { get; set; }
        // Collections
        public List<TripExpenseDto> TripExpenses { get; set; } = new List<TripExpenseDto>();
        public List<TripCheckpointDto> TripCheckpoints { get; set; } = new List<TripCheckpointDto>();

    }
}
