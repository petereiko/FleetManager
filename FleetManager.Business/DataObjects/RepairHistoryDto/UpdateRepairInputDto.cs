using FleetManager.Business.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.RepairHistoryDto
{
    public class UpdateRepairInputDto
    {
        public long RepairId { get; set; }
        public long VehicleId { get; set; }
        public long? DriverId { get; set; }
        public string Subject { get; set; } = "";
        public string? Notes { get; set; }
        public MaintenancePriority Priority { get; set; }
        public List<RepairItemUpdateDto> Items { get; set; } = new();
    }

    public class RepairItemUpdateDto
    {
        public long? Id { get; set; } 
        public int? PartCategoryId { get; set; }
        public int? PartId { get; set; }
        public string? CustomDescription { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }

}
