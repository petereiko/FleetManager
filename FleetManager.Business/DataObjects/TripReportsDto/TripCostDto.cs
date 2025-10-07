using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.TripReportsDto
{
    public class TripCostDto
    {
        public long TripId { get; set; }
        public string TripNumber { get; set; } = "";
        public decimal? EstimatedFuelCost { get; set; }
        public decimal? ActualFuelCost { get; set; }
        public decimal? Difference => (ActualFuelCost.HasValue && EstimatedFuelCost.HasValue) ? ActualFuelCost - EstimatedFuelCost : null;
    }
}
