using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Enums
{
    public enum DutyOfCareRecordType
    {
        [Description("Health Check")]
        HealthCheck = 0,
        [Description("Safety Training")]
        SafetyTraining,
        [Description("License Renewal")]
        LicenceRenewal,
        [Description("Drug Test")]
        DrugTest,
        [Description("Vision Test")]
        VisionTest,
        [Description("Compliance Audit")]
        ComplianceAudit,
        [Description("Vehicle Safety Briefing")]
        VehicleSafetyBriefing,
        Others
    }
}
