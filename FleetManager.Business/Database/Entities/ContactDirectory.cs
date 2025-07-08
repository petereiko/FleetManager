using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Database.Entities
{
    public class ContactDirectory : BaseEntity
    {
        public string ContactName { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }

        public string? VendorName { get; set; }
        public string? Address { get; set; }
        public long? CompanyBranchId { get; set; }
        public virtual CompanyBranch? CompanyBranch { get; set; }
        public int? CategoryId { get; set; }
        public virtual VendorCategory? Category { get; set; }

        public string? Services { get; set; }
    }

}
