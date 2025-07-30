using FleetManager.Business.Database.IdentityModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Database.Entities
{
    public class CompanyAdmin:BaseEntity
    {
        public int Id { get; set; }
        public long? CompanyId { get; set; }
        public virtual Company Company { get; set; }
        public long? CompanyBranchId { get; set; }
        public virtual CompanyBranch CompanyBranch { get; set; }
        public string? UserId { get; set; }
        public virtual ApplicationUser? User { get; set; }
    }
}
