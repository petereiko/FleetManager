using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace FleetManager.Business.Database.IdentityModels
{
    public class ApplicationUser : IdentityUser<string>
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool IsFirstLogin { get; set; }
        public DateTime? LastLoginDate { get; set; }
        public long? CompanyId { get; set; }

    }
}
