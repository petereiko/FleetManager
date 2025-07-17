using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Database.Entities
{
    public class Trip : BaseEntity
    {
        // FKs
        public long VehicleId { get; set; }
        public virtual Vehicle Vehicle { get; set; } = null!;

        public long DriverId { get; set; }
        public virtual Driver Driver { get; set; } = null!;

        // When the trip started and ended
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }

        // Distance travelled in this trip (e.g. in kilometers)
        public double Distance { get; set; }

        // (Optional) You can add more fields as you need them:
        public decimal? FuelUsed { get; set; }    // liters or gallons
        public string? StartLocation { get; set; }
        public string? EndLocation { get; set; }
        public string? Notes { get; set; }
    }

}
