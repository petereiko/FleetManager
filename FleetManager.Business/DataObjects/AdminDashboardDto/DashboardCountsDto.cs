using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.AdminDashboardDto
{
    public class DashboardCountsDto
    {
        public int TotalDrivers { get; set; }
        public int ActiveDrivers { get; set; }
        public int TotalVehicles { get; set; }
        public int ActiveVehicles { get; set; }
        public int AssignedVehicleCount { get; set; }
        public int OpenMaintenanceTickets { get; set; }
        public int OverdueMaintenanceTickets { get; set; }
        public int OpenFines { get; set; }
        public int VendorsCount { get; set; } // contact directory count
    }
}
