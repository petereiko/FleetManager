using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Database.Entities
{
    public class DriverVehicle:BaseEntity
    {
        public long? DriverId { get; set; }
        public virtual Driver? Driver {  get; set; }

        public long? VehicleId { get; set; }
        public virtual Vehicle? Vehicle { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
