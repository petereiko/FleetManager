using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects
{
    public class DriverVehicleListItemDto
    {
        public long Id { get; set; }
        public long DriverId { get; set; }
        public string DriverName { get; set; } = "";
        public long VehicleId { get; set; }
        public string VehicleMakeModel { get; set; } = "";
        public string PlateNo { get; set; } = "";
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
