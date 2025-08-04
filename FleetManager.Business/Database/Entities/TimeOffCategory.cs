using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FleetManager.Business.Database.Entities
{
    public class TimeOffCategory : BaseEntity
    {
        [Required, StringLength(100)]
        public string Name { get; set; } = null!;

        public string? Description { get; set; }

        public long? CompanyBranchId { get; set; }
        public virtual CompanyBranch? CompanyBranch { get; set; } = null!;

        // navigation
        //public virtual ICollection<TimeOff> TimeOffRequests { get; set; } = new List<TimeOff>();
    }
}
