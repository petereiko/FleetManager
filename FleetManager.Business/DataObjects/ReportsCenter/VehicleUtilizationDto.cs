using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.ReportsCenter
{
    public class VehicleUtilizationDto
    {
        public long VehicleId { get; set; }
        public string PlateNo { get; set; } = "";
        public decimal UsagePercentage { get; set; } // 0..100
        public TimeSpan IdleTime { get; set; }
        public TimeSpan Downtime { get; set; } // maintenance etc.
        public decimal RevenueHours { get; set; }
    }
}
