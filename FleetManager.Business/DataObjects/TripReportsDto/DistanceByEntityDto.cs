using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.TripReportsDto
{

    public class DistanceByEntityDto
    {
        public long EntityId { get; set; }
        public string EntityName { get; set; } = "";
        public decimal TotalDistance { get; set; }
        public int TripCount { get; set; }
    }
}
