using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FleetManager.Business.Enums;

namespace FleetManager.Business.Database.Entities
{
    public class Driver:BaseEntity
    {
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
