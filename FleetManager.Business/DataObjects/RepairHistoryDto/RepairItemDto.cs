using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.RepairHistoryDto
{
    public class RepairItemDto
    {
        public long Id { get; set; }
        public int? PartId { get; set; }
        public string? PartName { get; set; }
        public string? PartCategoryName { get; set; }
        public string? CustomDescription { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
    }

}
