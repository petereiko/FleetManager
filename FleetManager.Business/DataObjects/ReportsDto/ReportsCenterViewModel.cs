using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.ReportsDto
{
    public class ReportsCenterViewModel
    {
        public List<ReportMetadata> MetadataList { get; set; } = new();
        // Driver summary
        public int TotalDriverCount { get; set; }
        public int ActiveDriverCount { get; set; }

        // Vehicle summary
        public int TotalVehicleCount { get; set; }
        public int ActiveVehicleCount { get; set; }
    }

}
