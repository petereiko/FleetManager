using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.TripsDto
{
    public class DashboardSeriesDto
    {
        // Status display name -> count
        public Dictionary<string, int> StatusCounts { get; set; } = new();
        // Ordered oldest->newest 7-day list
        public List<DailySeriesPoint> SevenDayTrend { get; set; } = new();
    }


    public class DailySeriesPoint
    {
        public DateTime Date { get; set; }
        public int Count { get; set; }
    }
}
