using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.AdminDashboardDto
{
    public class PartCategorySpendDto
    {
        public int? VehiclePartCategoryId { get; set; }
        public decimal Spend { get; set; }
    }
}
