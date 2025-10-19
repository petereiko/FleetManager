using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.DashboardDriverDto
{
    public class SafetyMetricsDto
    {
        public double SafeDrivingScore { get; set; }
        public double SpeedCompliancePercent { get; set; }
        public double OnTimeDeliveryPercent { get; set; }
        public int DaysAccidentFree { get; set; }
        public int ViolationsCount { get; set; }
    }
}
