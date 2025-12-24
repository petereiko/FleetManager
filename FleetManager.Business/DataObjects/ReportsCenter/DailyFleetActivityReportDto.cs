using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.ReportsCenter
{
    public class DailyFleetActivityReportDto
    {
        public DateTime Date { get; set; }
        public int TotalVehicles { get; set; }
        public int ActiveVehicles { get; set; }
        public int IdleVehicles { get; set; }
        public int TripsCompleted { get; set; }
        public decimal TotalDistanceKm { get; set; }
        public List<VehicleActivityDto> VehicleActivities { get; set; } = new();
    }
}
