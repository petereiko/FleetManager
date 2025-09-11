using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Database.Entities.RepairHistory
{
    public class RepairItem : BaseEntity
    {
        public long RepairId { get; set; }
        public virtual Repair Repair { get; set; } = null!;

        public int? VehiclePartCategoryId { get; set; }
        public virtual VehiclePartCategory? VehiclePartCategory { get; set; }

        public int? VehiclePartId { get; set; }
        public virtual VehiclePart? VehiclePart { get; set; }

        public string? CustomPartDescription { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }

        public decimal LineTotal => Quantity * UnitPrice;
    }

}
