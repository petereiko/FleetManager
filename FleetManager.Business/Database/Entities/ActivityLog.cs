using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Database.Entities
{
    public class ActivityLog:BaseEntity
    {
        public string? Action { get; set; } 
        public string? Description { get; set; }
        public long? CompanyId { get; set; }
        public virtual Company Company { get; set; }
        public long? CompanyBranchId { get; set; }
        public virtual CompanyBranch? CompanyBranch { get; set; }
    }
}
