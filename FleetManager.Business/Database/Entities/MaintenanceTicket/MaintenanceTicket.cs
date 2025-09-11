using FleetManager.Business.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Database.Entities.MaintenanceTicket
{
    public class MaintenanceTicket:BaseEntity
    {
        public long DriverId { get; set; }
        public virtual Driver Driver { get; set; }
        public long VehicleId { get; set; }
        public virtual Vehicle Vehicle { get; set; }
        public long? CompanyBranchId { get; set; }
        public virtual CompanyBranch? CompanyBranch { get; set; }
        // A short description of the overall issue
        public string Subject { get; set; } = "";
        public string? Notes { get; set; }

        // Line‐items: parts, quantities, unit prices
        public virtual ICollection<MaintenanceTicketItem> Items { get; set; }
            = new List<MaintenanceTicketItem>();

        // Admin investigation & resolution
        public TicketStatus Status { get; set; } = TicketStatus.Pending;

        public MaintenancePriority? Priority { get; set;}
        public DateTime? ResolvedAt { get; set; }
        public string? AdminNotes { get; set; }

        // Link to the automatically generated invoice
        //public long? InvoiceId { get; set; }
        public virtual Invoice? Invoice { get; set; }
    }

}
