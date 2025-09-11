using FleetManager.Business.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.RepairHistoryDto
{
    public class RepairInvoiceDto
    {
        public long Id { get; set; }
        public long RepairId { get; set; }
        public DateTime InvoiceDate { get; set; }
        public InvoiceStatus Status { get; set; }
        public decimal TotalAmount { get; set; }
        public List<RepairInvoiceItemDto> Items { get; set; } = new();
    }

}
