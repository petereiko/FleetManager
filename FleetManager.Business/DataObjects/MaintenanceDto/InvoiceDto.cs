using FleetManager.Business.Database.Entities;
using FleetManager.Business.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.MaintenanceDto
{
    public class InvoiceDto:BaseEntity
    {
        public long Id { get; set; }
        public long TicketId { get; set; }
        public long CompanyBranchId { get; set; }
        public DateTime InvoiceDate { get; set; }
        public InvoiceStatus Status { get; set; }
        public decimal TotalAmount { get; set; }
        public List<InvoiceItemDto> Items { get; set; } = new();
    }
}
