using FleetManager.Business.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.ViewModels
{
    public class DutyOfCareViewModel
    {
        public DutyOfCareInputModel Input { get; set; } = new();

        public IEnumerable<SelectListItem> DutyOfCareType { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> DutyOfCareStatus { get; set; } = new List<SelectListItem>();
    }




    public class  DutyOfCareInputModel
    {
        public long? Id { get; set; }

        public long DriverId { get; set; }
        //public string DriverName { get; set; }

        //public string VehicleDescription { get; set; }
        public long VehicleId { get; set; }
        public DateTime Date { get; set; }

        // Vehicle Responsibility
        public bool VehiclePreCheckCompleted { get; set; }
        public string VehicleConditionNotes { get; set; } = string.Empty;

        // Health & Fitness
        public bool IsFitToDrive { get; set; }
        public string HealthDeclarationNotes { get; set; } = string.Empty;

        // Legal Compliance
        public bool HasValidLicense { get; set; }
        public bool IsAwareOfCompanyPolicies { get; set; }
        public bool HasReviewedDrivingHours { get; set; }

        // Fatigue Management
        public TimeSpan LastRestPeriod { get; set; }
        public bool ReportsFatigue { get; set; }

        // Incident / Hazard Awareness
        public bool ReportsVehicleIssues { get; set; }
        public string ReportedIssuesDetails { get; set; } = string.Empty;

        // Consent & Declaration
        public bool ConfirmsAccuracyOfInfo { get; set; }
        public DateTime DeclarationTimestamp { get; set; }

        // Record metadata
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime? ModifiedDate { get; set; }
        public string? ModifiedBy { get; set; }

        // Enums
        public DutyOfCareRecordType DutyOfCareRecordType { get; set; }
        public DriverDutyOfCareStatus DutyOfCareStatus { get; set; }
    }
}
