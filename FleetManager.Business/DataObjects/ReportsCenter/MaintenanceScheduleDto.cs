using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.ReportsCenter
{
    public class MaintenanceScheduleDto
    {
        public long VehicleId { get; set; }
        public string PlateNo { get; set; } = "";
        public DateTime? NextMaintenanceDate { get; set; }
        public bool IsOverdue => NextMaintenanceDate.HasValue && NextMaintenanceDate.Value < DateTime.UtcNow;
        public decimal ProjectedCost { get; set; }
    }
}
