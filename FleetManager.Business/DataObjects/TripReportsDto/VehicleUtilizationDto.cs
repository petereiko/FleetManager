using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.TripReportsDto
{
    public class VehicleUtilizationDto
    {
        public long VehicleId { get; set; }
        public string VehiclePlateNo { get; set; } = "";
        public TimeSpan TotalUsageHours { get; set; } // total time in use within date range
        public decimal TotalDistance { get; set; }
        public int TripCount { get; set; }
    }
}
