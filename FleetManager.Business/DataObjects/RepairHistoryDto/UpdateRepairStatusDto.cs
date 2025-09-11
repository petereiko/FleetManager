using FleetManager.Business.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.RepairHistoryDto
{
    public class UpdateRepairStatusDto
    {
        public long RepairId { get; set; }
        public RepairStatus NewStatus { get; set; }
        public InvoiceStatus? InvoiceStatus { get; set; }
        public string? AdminNotes { get; set; } // optional
    }

}
