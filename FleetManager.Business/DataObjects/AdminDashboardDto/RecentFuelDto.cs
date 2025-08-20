using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.AdminDashboardDto
{
    public class RecentFuelDto
    {
        public long FuelLogId { get; set; }
        public long? VehicleId { get; set; }
        public string VehiclePlateNo { get; set; } = "";
        public long? DriverId { get; set; }
        public decimal Volume { get; set; }
        public decimal Cost { get; set; }
        public int? Odometer { get; set; }
        public DateTime Date { get; set; }
    }
}
