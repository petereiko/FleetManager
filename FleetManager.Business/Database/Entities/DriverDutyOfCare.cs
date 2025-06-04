using FleetManager.Business.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Database.Entities
{
    public class DriverDutyOfCare:BaseEntity
    {
        public long? DriverId { get; set; }  
        public virtual Driver Driver { get; set; }
        public DateTime Date { get; set; } = DateTime.UtcNow;

        // Vehicle Responsibility
        public long? VehicleId { get; set; }
        public virtual Vehicle Vehicle { get; set; }
        public bool VehiclePreCheckCompleted { get; set; }
        public string VehicleConditionNotes { get; set; }

        // Health & Fitness
        public bool IsFitToDrive { get; set; }
        public string HealthDeclarationNotes { get; set; }

        // Legal Compliance
        public bool HasValidLicense { get; set; }
        public bool IsAwareOfCompanyPolicies { get; set; }
        public bool HasReviewedDrivingHours { get; set; }

        // Fatigue Management
        public TimeSpan LastRestPeriod { get; set; }
        public bool ReportsFatigue { get; set; }

        // Incident / Hazard Awareness
        public bool ReportsVehicleIssues { get; set; }
        public string ReportedIssuesDetails { get; set; }

        // Consent & Declaration
        public bool ConfirmsAccuracyOfInfo { get; set; }
        public DateTime DeclarationTimestamp { get; set; } = DateTime.UtcNow;

        // Admin
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string CreatedBy { get; set; }

        public DutyOfCareRecordType DutyOfCareRecordType { get; set; }
        public DriverDutyOfCareStatus DutyOfCareStatus { get; set; }
    }

}
