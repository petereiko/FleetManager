using FleetManager.Business.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.MaintenanceDto
{
    public class MaintenanceTicketDto
    {
        public long Id { get; set; }
        public long DriverId { get; set; }
        public string DriverName { get; set; } = "";
        public long VehicleId { get; set; }
        public string VehicleDescription { get; set; } = "";
        public long CompanyBranchId { get; set; }
        public string Subject { get; set; } = "";
        public string? Notes { get; set; }
        public TicketStatus Status { get; set; }
        public MaintenancePriority? Priority { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public string? AdminNotes { get; set; }
        public List<MaintenanceTicketItemDto> Items { get; set; } = new();
        //public long? InvoiceId { get; set; }
        public InvoiceDto? Invoice { get; set; }
    }
}
