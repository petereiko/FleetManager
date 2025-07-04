using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.VendorDto
{
    public class VendorOnboardingDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = null!;

        [Required]
        [Phone]
        public string PhoneNumber { get; set; } = null!;
        [Required]
        public string VendorName { get; set; }

        [Required]
        public string FirstName { get; set; }
        [Required]
        public string LastName { get; set; }

        [Required]
        public int VendorCategoryId { get; set; }

        [Required]
        public string VendorServiceOffered { get; set; }

        [Required]
        public string ContactPerson { get; set; } = null!;

        [Required]
        public string ContactPersonPhone { get; set; } = null!;

        public string? Address { get; set; }
        public string? City {  get; set; }
        public long? StateId { get; set; }
        public string? CACRegistrationNo { get; set; }
        public string? TaxIdNumber { get; set; }
    }
}
