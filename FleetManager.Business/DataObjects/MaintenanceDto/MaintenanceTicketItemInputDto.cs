using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.MaintenanceDto
{
    public class MaintenanceTicketItemInputDto
    {
        public int? PartCategoryId { get; set; }
        public int? PartId { get; set; }
        public string? CustomDescription { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}
