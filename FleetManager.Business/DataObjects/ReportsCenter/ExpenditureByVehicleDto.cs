using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.ReportsCenter
{
    public class ExpenditureByVehicleDto
    {
        public long VehicleId { get; set; }
        public string PlateNo { get; set; } = "";
        public decimal Amount { get; set; }
    }
}
