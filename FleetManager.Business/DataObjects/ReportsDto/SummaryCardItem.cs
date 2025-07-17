using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.ReportsDto
{
    public class SummaryCardItem
    {
        public string Key { get; set; }
        public object Value { get; set; }
        public string IconClass { get; set; }  // e.g., fa-users
        public string BackgroundClass { get; set; } // e.g., bg-primary
        public bool IsPercentage { get; set; } = false;
    }
}
