using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Enums
{
    public enum TicketStatus
    {
        Pending = 1,       // New, awaiting admin review
        Approved,      // Admin has approved the invoice
        Rejected,      // Admin denied the request
        InProgress,    // Being serviced
        Resolved       // Completed
    }

}
