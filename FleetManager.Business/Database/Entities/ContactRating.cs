using FleetManager.Business.Database.IdentityModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Database.Entities
{
 
    public class ContactRating : BaseEntity
    {
        // 1..5
        public int Rating { get; set; }

        // Optional textual feedback
        public string? Comment { get; set; }

        // Link to the contact
        public long ContactDirectoryId { get; set; }
        public virtual ContactDirectory ContactDirectory { get; set; } = null!;
        public long? CompanyBranchId { get; set; }
        public virtual CompanyBranch? CompanyBranch { get; set; }

        public string UserId { get; set; } 
        public virtual ApplicationUser? User {get; set;}

        // Optionally store a display name / role at the time of rating for audit
        public string? UserDisplayName { get; set; }
    }

}
