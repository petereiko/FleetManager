using FleetManager.Business.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.MaintenanceDto
{
    public class UpdateTicketStatusDto
    {
        public long TicketId { get; set; }
        public TicketStatus NewStatus { get; set; }
        public InvoiceStatus? InvoiceStatus { get; set; }
        public string? AdminNotes { get; set; }
    }
}
