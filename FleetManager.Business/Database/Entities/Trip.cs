using FleetManager.Business.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Database.Entities
{
    public class Trip : BaseEntity
    {
        public string TripNumber { get; set; } // Auto-generated unique identifier
        public long CompanyBranchId { get; set; }
        public virtual CompanyBranch CompanyBranch { get; set; }

        public long CompanyId { get; set; }
        public virtual Company Company { get; set; }

        // Assignment Details
        public long? DriverId { get; set; }
        public virtual Driver Driver { get; set; }

        public long VehicleId { get; set; }
        public virtual Vehicle Vehicle { get; set; }

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

        // Trip Metrics
        public decimal? EstimatedDistance { get; set; } // in KM
        public decimal? ActualDistance { get; set; }
        public decimal? EstimatedFuelCost { get; set; }
        public decimal? ActualFuelCost { get; set; }

        // Odometer Readings
        public int? StartOdometer { get; set; }
        public int? EndOdometer { get; set; }

        // Status & Priority
        public TripStatus Status { get; set; }
        public TripPriority Priority { get; set; }

        // Assignment
        public string? AssignedBy { get; set; } // UserId of admin who assigned
        public DateTime? AssignedDate { get; set; }

        // Approval Workflow (Optional)
        public bool RequiresApproval { get; set; }
        public bool? IsApproved { get; set; }
        public string? ApprovedBy { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public string? RejectionReason { get; set; }

        // Additional Information
        public string? Notes { get; set; }
        public string? CancellationReason { get; set; }
        public DateTime? CancellationDate { get; set; }
        

        // Navigation Properties
        public virtual ICollection<TripExpense> TripExpenses { get; set; } = new HashSet<TripExpense>();
        public virtual ICollection<TripDocument> TripDocuments { get; set; } = new HashSet<TripDocument>();
        public virtual ICollection<TripCheckpoint> TripCheckpoints { get; set; } = new HashSet<TripCheckpoint>();


        //Take them out later
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public double Distance { get; set; }
    }
}
