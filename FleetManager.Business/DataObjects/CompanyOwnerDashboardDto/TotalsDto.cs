using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.CompanyOwnerDashboardDto
{
    public class TotalsDto
    {
        public int TotalDrivers { get; set; }
        public int TotalVehicles { get; set; }
        public int AssignedVehicles { get; set; }

        public decimal FuelSpend { get; set; }
        public decimal MaintenanceSpend { get; set; }
        public decimal FinesSpend { get; set; }
        public decimal TotalSpend { get; set; }
    }
}
