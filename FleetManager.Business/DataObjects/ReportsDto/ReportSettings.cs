using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.ReportsDto
{
    public class ReportSettings
    {
        public int LicenseExpiryThresholdDays { get; set; } = 30;
        public int MaintenanceDueDays { get; set; } = 90;
        public int MaintenanceWarningDays { get; set; } = 60;
        public int CacheDurationMinutes { get; set; } = 15;
        public decimal LicenseExpiredPenalty { get; set; } = 30;
        public decimal LicenseExpiringPenalty { get; set; } = 10;
    }
}
