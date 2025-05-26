using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FleetManager.Business.Database.IdentityModels;
using FleetManager.Business.Enums;

namespace FleetManager.Business.Database.Entities
{
    public class Driver:BaseEntity
    {
        public string UserId { get; set; }
        public virtual ApplicationUser User { get; set; }
        public string Address { get; set; }
        public DateTime DateOfBirth { get; set; }
        public Gender Gender { get; set; }
        public EmploymentStatus EmploymentStatus { get; set; } 
        public string? LicenseNumber { get; set; }
        public DateTime? LicenseExpiryDate { get; set; }
        public long? CompanyBranchId { get; set; }
        public LicenseCategory LicenseCategory { get; set; }
        public ShiftStatus ShiftStatus { get; set; } // Available,On Duty,Off Duty etc
        public DateTime? LastSeen { get; set; }
    }
}
