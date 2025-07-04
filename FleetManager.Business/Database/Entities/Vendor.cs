using FleetManager.Business.Database.IdentityModels;
using FleetManager.Business.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Database.Entities
{
    public class Vendor:BaseEntity
    {
        public string UserId { get; set; }
        public virtual ApplicationUser? User { get; set; }
        public string VendorName { get; set; }
        public string FirstName { get; set;}
        public string LastName { get; set; }
        public int VendorCategoryId { get; set; }
        public virtual VendorCategory? VendorCategory { get; set; }
        public string? ContactPerson { get; set; }
        public string? ContactPersonPhone { get; set; } = null!;
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public long? StateId { get; set; }
        public virtual State? State { get; set; }
        public string VendorServiceOffered { get; set; }
        public string? CACRegistrationNo { get; set; }
        public string? TaxIdNumber { get; set; }
        public virtual ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
    }
}
