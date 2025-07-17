using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Database.Entities.MaintenanceTicket
{
    public class MaintenanceTicketItem:BaseEntity
    {

        // Parent ticket
        public long TicketId { get; set; }
        public virtual MaintenanceTicket Ticket { get; set; }

        // Optional dropdown part reference

        public int? VehiclePartCategoryId { get; set; }
        public virtual VehiclePartCategory? VehiclePartCategory { get; set; }
        public int? VehiclePartId { get; set; }
        public virtual VehiclePart? VehiclePart { get; set; }


        // If the part isn’t in the dropdown:
        public string? CustomPartDescription { get; set; }

        // How many and at what unit price
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; } = 0m;

        // Computed (not persisted) convenience:
        public decimal LineTotal => Quantity * UnitPrice;
    }

}
