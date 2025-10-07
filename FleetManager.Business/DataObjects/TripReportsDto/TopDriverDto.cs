using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.TripReportsDto
{
    public class TopDriverDto
    {
        public long DriverId { get; set; }
        public string DriverName { get; set; } = "";
        public int TripCount { get; set; }
        public decimal TotalDistance { get; set; }
        public decimal TotalFuelCost { get; set; }
    }
}
