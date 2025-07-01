using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Enums
{
    public enum RentalStatus
    {
        Reserved = 1,
        Active,
        Completed,
        Late,
        Cancelled
    }
}
