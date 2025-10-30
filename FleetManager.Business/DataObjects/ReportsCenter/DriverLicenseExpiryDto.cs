using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.ReportsCenter
{
    public class DriverLicenseExpiryDto
    {
        public long DriverId { get; set; }
        public string DriverName { get; set; } = "";
        public string LicenseNumber { get; set; } = "";
        public DateTime? LicenseExpiryDate { get; set; }
        public string Status { get; set; } = ""; // e.g. Expired / ExpiringSoon / Valid
    }
}
