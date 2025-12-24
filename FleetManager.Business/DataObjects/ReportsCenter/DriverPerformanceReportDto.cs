using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.ReportsCenter
{
    public class DriverPerformanceReportDto
    {
        public long DriverId { get; set; }
        public string DriverName { get; set; } = "";
        public int TripsCount { get; set; }
        public decimal TotalDistanceKm { get; set; }
        public double TotalHours { get; set; } // in hours
        public int IncidentsCount { get; set; }
    }
}
