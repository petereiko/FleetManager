using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.VendorDto
{
    public class VendorDto
    {
        public long Id { get; set; }
        public string VendorName { get; set; } = null!;
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int VendorCategoryId { get; set; }
        public string VendorCategoryName { get; set; } = null!;
        public string VendorServiceOffered { get; set; }
        public string ContactPerson { get; set; } = null!;
        public string ContactPersonPhone { get; set; } = null!;
        public string? Address { get; set; }
        public string? City { get; set; }
        public long? StateId { get; set; }
        public string? StateName { get; set; }
        [EmailAddress]
        public string? Email { get; set; }
        [Phone]
        public string PhoneNumber { get; set; } = null!;
        public string? CACRegistrationNo { get; set; }
        public string? TaxIdNumber { get; set; }

        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public string? ModifiedBy { get; set; }
    }
}
