using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Enums
{
    public enum MaintenanceType
    {
        Preventive = 1,
        Corrective = 2,
        Inspection = 3,
        ReportedIssue = 4,
        Emergency = 5,
        ExternalService = 6
    }

}
