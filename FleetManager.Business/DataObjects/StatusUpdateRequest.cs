using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects
{
    public class StatusUpdateRequest
    {
        public long VehicleId { get; set; }
        public int NewStatus { get; set; }
    }
}
