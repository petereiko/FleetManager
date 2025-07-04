using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.VendorDto
{
    public class VendorDashboardViewModel
    {
        public long Id { get; set; }

        // Basic Profile
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string VendorName { get; set; }

        // Contact
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string ContactPerson { get; set; }
        public string ContactPersonPhone { get; set; }

        // Business Info
        public string VendorCategoryName { get; set; }
        public string VendorServiceOffered { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string StateName { get; set; }

        // Registration Info
        public string CACRegistrationNo { get; set; }
        public string TaxIdNumber { get; set; }

        // Status
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }

        // Future Enhancements
        public int ProfileViews { get; set; } // optional
        public bool IsVerified { get; set; }  // optional
    }

}
