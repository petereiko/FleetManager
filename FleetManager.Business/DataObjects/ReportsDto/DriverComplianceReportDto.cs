using FleetManager.Business.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.ReportsDto
{
    public class DriverComplianceReportDto
    {
        public long DriverId { get; set; }
        public string DriverName { get; set; }
        public string LicenseNumber { get; set; }
        public DateTime? LicenseExpiryDate { get; set; }
        public int DaysUntilLicenseExpiry { get; set; }
        public LicenseCategory LicenseCategory { get; set; }
        public EmploymentStatus EmploymentStatus { get; set; }
        public bool IsCompliant { get; set; }
        public List<string> ComplianceIssues { get; set; } = new();
        public DateTime LastActiveDate { get; set; }
    }

}
