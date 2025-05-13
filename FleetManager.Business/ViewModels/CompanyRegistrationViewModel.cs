using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.ViewModels
{
    public class CompanyRegistrationViewModel
    {
        public string? Name { get; set; }
        public string? RegistrationNumber { get; set; }
        public string? Address { get; set; }
        public DateTime? DateOfIncorporation { get; set; }
        public string? State { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? ContactPersonName { get; set; }
        public string? ContactPersonPhone { get; set; }
        public string? ContactPersonEmail { get; set; }
        public string? Website { get; set; }
        public string? LogoUrl { get; set; }
        public bool IsVerified { get; set; }
        public int Token { get; set; }
    }
}
