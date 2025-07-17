using FleetManager.Business.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Database.Entities.MaintenanceTicket
{
    public class Invoice:BaseEntity
    {
        public long? CompanyBranchId { get; set; }
        public virtual CompanyBranch? CompanyBranch { get; set; }

        // Link back to the ticket
        public long TicketId { get; set; }
        public virtual MaintenanceTicket Ticket { get; set; }

        // Invoice metadata
        public DateTime InvoiceDate { get; set; } = DateTime.UtcNow;
        public InvoiceStatus Status { get; set; } = InvoiceStatus.Pending;

        // Sum of all line‐items
        public decimal TotalAmount { get; set; }

        public virtual ICollection<InvoiceItem> Items { get; set; }
            = new List<InvoiceItem>();
    }

}
