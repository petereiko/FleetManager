using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Enums
{
    public enum InvoiceStatus
    {
        Pending = 1,   // Awaiting payment
        Paid,      // Completed
        Cancelled  // Voided or denied
    }

}
