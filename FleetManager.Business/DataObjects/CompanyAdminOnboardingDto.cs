using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects
{
    public class CompanyAdminOnboardingDto
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }           // Used as username
        public string PhoneNumber { get; set; }
        public string TemporaryPassword { get; set; }

        public long CompanyId { get; set; }
        public long? CompanyBranchId { get; set; }
    }

}
