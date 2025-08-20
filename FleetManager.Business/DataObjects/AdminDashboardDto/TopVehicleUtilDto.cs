using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.AdminDashboardDto
{
    public class TopVehicleUtilDto
    {
        public long VehicleId { get; set; }
        public string PlateNo { get; set; } = "";
        public decimal UtilizationPercent { get; set; }
        public int AssignedDays { get; set; }
    }

}
