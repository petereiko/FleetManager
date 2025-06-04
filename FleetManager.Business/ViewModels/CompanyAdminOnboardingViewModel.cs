using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.ViewModels
{
    public class CompanyAdminOnboardingViewModel
    {
        [Required, EmailAddress] public string Email { get; set; }
        [Required] public string PhoneNumber { get; set; }
        [Required] public string FirstName { get; set; }
        [Required] public string LastName { get; set; }

        [Required] public long CompanyBranchId { get; set; }
        public long? CompanyId { get; set; }

        public IEnumerable<SelectListItem> Branches { get; set; }=Enumerable.Empty<SelectListItem>();   
    }

}
