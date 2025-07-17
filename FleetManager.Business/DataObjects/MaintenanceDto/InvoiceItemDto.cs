using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.MaintenanceDto
{
    public class InvoiceItemDto
    {
        public long Id { get; set; }
        public long? PartId { get; set; }
        public string PartName { get; set; } = "";
        public string? PartCategory { get; set; }
        public string? CustomPartDescription { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
    }
}
