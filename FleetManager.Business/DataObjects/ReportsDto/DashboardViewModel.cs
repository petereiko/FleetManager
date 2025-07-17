using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.ReportsDto
{
    public class DashboardViewModel
    {
        // Vehicle Summary
        public int TotalVehicles { get; set; }
        public int ActiveVehicles { get; set; }

        // Driver Summary
        public int TotalDrivers { get; set; }
        public int ActiveDrivers { get; set; }
        public int ExpiringLicenses { get; set; }

        // Fuel Summary
        public decimal TotalFuelCost { get; set; }
        public decimal TotalFuelVolume { get; set; }
        public int TotalFillUps { get; set; }
        public double AverageFuelEfficiency { get; set; }

        // Compliance Summary
        public int ExpiredInsurance { get; set; }
        public int ExpiredRoadWorthy { get; set; }

        // For Chart (Driver Compliance)
        public int CompliantDrivers => ActiveDrivers - ExpiringLicenses;
        public int NonCompliantDrivers => TotalDrivers - ActiveDrivers;
    }

}
