using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.DashboardDriverDto
{
    public class WeeklyPerformanceDto
    {
        public List<string> Labels { get; set; } = new List<string>();
        public List<double> Distances { get; set; } = new List<double>();
        public List<double>? SafetyScores { get; set; }
    }
}
