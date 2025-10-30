using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.ReportsCenter
{
    public class OvertimeAnalysisDto
    {
        public long DriverId { get; set; }
        public string DriverName { get; set; } = "";
        public double OvertimeHours { get; set; }
        public decimal Cost { get; set; }
    }
}
