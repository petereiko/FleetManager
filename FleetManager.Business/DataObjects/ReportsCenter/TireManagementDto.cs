using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.ReportsCenter
{
    public class TireManagementDto
    {
        public long VehicleId { get; set; }
        public string PlateNo { get; set; } = "";
        public int KilometersSinceReplacement { get; set; }
        public int RecommendedLifespanKm { get; set; }
        public bool NeedsReplacement => KilometersSinceReplacement >= RecommendedLifespanKm;
        public decimal ReplacementCost { get; set; }
    }
}
