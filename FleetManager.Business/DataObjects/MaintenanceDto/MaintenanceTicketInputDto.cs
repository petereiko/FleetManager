using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FleetManager.Business.Enums;

namespace FleetManager.Business.DataObjects.MaintenanceDto
{
    public class MaintenanceTicketInputDto
    {
        public long DriverId { get; set; }
        public long VehicleId { get; set; }
        public string Subject { get; set; } = "";
        public string? Notes { get; set; }
        public MaintenancePriority? Priority { get; set; }
        public List<MaintenanceTicketItemInputDto> Items { get; set; } = new();
    }
}
