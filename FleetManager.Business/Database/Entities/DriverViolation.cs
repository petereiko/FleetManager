using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Database.Entities
{
    public class DriverViolation : BaseEntity
    {
        public long DriverId { get; set; }
        public virtual Driver Driver { get; set; } = null!;

        /// <summary>Description of the violation (e.g. "Speeding over 120km/h")</summary>
        public string Description { get; set; } = null!;
        public DateTime ViolationDate { get; set; }

        /// <summary>Severity rating (1 = low, 5 = critical)</summary>
        public int Severity { get; set; }
        public bool Resolved { get; set; }

        /// <summary>Optional notes by admin</summary>
        public string? Notes { get; set; }

        /// <summary>Who reported the violation (system, supervisor, etc.)</summary>
        public string? ReportedBy { get; set; }
    }

}
