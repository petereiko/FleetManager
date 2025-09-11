using FleetManager.Business.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Database.Entities.RepairHistory
{
    public class RepairInvoice : BaseEntity
    {
        public long RepairId { get; set; }
        public virtual Repair Repair { get; set; } = null!;

        public long? CompanyBranchId { get; set; }
        public virtual CompanyBranch? CompanyBranch { get; set; }

        public DateTime InvoiceDate { get; set; } = DateTime.UtcNow;
        public InvoiceStatus Status { get; set; } = InvoiceStatus.Pending;
        public decimal TotalAmount { get; set; }

        public virtual ICollection<RepairInvoiceItem> Items { get; set; } = new List<RepairInvoiceItem>();
    }

}
