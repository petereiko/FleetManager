using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Enums
{
    public enum DriverDutyOfCareStatus
    {
        [Description("Compliant")]
        Compliant = 0,
        [Description("Pending Review")]
        PendingReview,
        [Description("Action Required")]
        ActionRequired,
        [Description("Expired")]
        Expired,
        [Description("Upcoming Renewal")]
        UpcomingRenewal
    }
}
