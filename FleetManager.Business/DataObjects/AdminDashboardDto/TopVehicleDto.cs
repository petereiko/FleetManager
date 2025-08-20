using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.AdminDashboardDto
{
    public class TopVehicleDto
    {
        public long VehicleId { get; set; }
        public string PlateNo { get; set; } = "";
        public string VehicleName { get; set; } = "";
        public decimal TotalVolume { get; set; }
        public decimal TotalCost { get; set; }
    }
}
