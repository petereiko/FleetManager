using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.ReportsCenter
{
    public class ReportSummaryViewModel
    {
        public int TotalTrips { get; set; }
        public decimal TotalFuelCost { get; set; }
        public int ActiveVehicles { get; set; }
        public int TotalVehicles { get; set; }
        public int TotalIncidents { get; set; }
    }
}
