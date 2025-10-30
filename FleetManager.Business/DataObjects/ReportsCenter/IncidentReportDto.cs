using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.ReportsCenter
{
    public class IncidentReportDto
    {
        public long IncidentId { get; set; }
        public DateTime Date { get; set; }
        public string Type { get; set; } = "";
        public string Location { get; set; } = "";
        public string Description { get; set; } = "";
        public string Impact { get; set; } = "";
        public long? VehicleId { get; set; }
        public string VehiclePlate { get; set; } = "";
        public long? DriverId { get; set; }
        public string DriverName { get; set; } = "";
    }
}
