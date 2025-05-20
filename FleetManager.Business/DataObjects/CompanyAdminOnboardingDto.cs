using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects
{
    public class CompanyAdminOnboardingDto
    {
        [Required]
        public string FirstName { get; set; }
        [Required]
        public string LastName { get; set; }
        [Required]
        public string Email { get; set; }
        [Required]// Used as username
        public string PhoneNumber { get; set; }

        public long CompanyId { get; set; }
        [Required]
        public long? CompanyBranchId { get; set; }
    }

}
