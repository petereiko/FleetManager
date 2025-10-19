using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.DashboardDriverDto
{
    public class AssignedVehicleDto
    {
        public long VehicleId { get; set; }
        public string MakeModel { get; set; } = string.Empty;
        public string? FleetId { get; set; }
        public string? PlateNo { get; set; }
        public int? Mileage { get; set; }
        public int FuelLevelPercent { get; set; } // if not available, 0
        public string EngineHealth { get; set; } = string.Empty;
        public string TireCondition { get; set; } = string.Empty;
    }
}
