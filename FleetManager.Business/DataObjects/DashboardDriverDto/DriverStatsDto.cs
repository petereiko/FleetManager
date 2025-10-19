using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.DashboardDriverDto
{
    public class DriverStatsDto
    {
        public double TotalMilesDriven { get; set; }
        public double HoursThisMonth { get; set; }
        public double SafetyScorePercent { get; set; }
        public int DeliveriesCompleted { get; set; }
    }
}
