using FleetManager.Business.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.RepairHistoryDto
{
    public class RepairInputDto
    {
        public long VehicleId { get; set; }
        public long? DriverId { get; set; }
        public string Subject { get; set; } = "";
        public string? Notes { get; set; }
        public MaintenancePriority Priority { get; set; } = MaintenancePriority.Moderate;
        public List<RepairItemInputDto> Items { get; set; } = new();
    }

    public class RepairItemInputDto
    {
        public int? PartCategoryId { get; set; }
        public int? PartId { get; set; }
        public string? CustomDescription { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }

}
