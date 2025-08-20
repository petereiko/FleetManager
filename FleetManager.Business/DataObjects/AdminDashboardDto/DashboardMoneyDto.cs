using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.AdminDashboardDto
{
    public class DashboardMoneyDto
    {
        public decimal TotalSpend { get; set; }
        public decimal FuelSpend { get; set; }
        public decimal MaintenanceSpend { get; set; }
        public decimal FinesSpend { get; set; }
        public decimal CostPerKm { get; set; }             // NGN per km
        public decimal AvgFuelPerKm { get; set; }          // liters per km
        public double AvgTimeToResolveMaintenanceHours { get; set; }
        public decimal UtilizationRatePercent { get; set; } // average % (0-100)
    }
}
