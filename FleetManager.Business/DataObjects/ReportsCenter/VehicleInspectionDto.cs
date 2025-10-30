using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.ReportsCenter
{
    public class VehicleInspectionDto
    {
        public long InspectionId { get; set; }
        public DateTime Date { get; set; }
        public long VehicleId { get; set; }
        public string PlateNo { get; set; } = "";
        public string Inspector { get; set; } = "";
        public bool Passed { get; set; }
        public string Notes { get; set; } = "";
    }
}
