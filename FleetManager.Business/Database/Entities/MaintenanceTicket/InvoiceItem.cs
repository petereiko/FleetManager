using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Database.Entities.MaintenanceTicket
{
    public class InvoiceItem:BaseEntity
    {
        // Parent invoice
        public long InvoiceId { get; set; }
        public virtual Invoice Invoice { get; set; }

        // Description (copy from ticket item)
        public string? Description { get; set; } = "";

        public int? VehiclePartCategoryId { get; set; }
        public virtual VehiclePartCategory? VehiclePartCategory { get; set; }
        public int? VehiclePartId { get; set; }
        public virtual VehiclePart? VehiclePart { get; set; }

        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }

        // Computed field
        public decimal LineTotal => Quantity * UnitPrice;
    }

}
