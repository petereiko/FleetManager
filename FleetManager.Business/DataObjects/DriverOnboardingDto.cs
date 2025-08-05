using FleetManager.Business.Enums;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects
{
    public class DriverOnboardingDto
    {
        // User account info
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }    // used as username
        public string PhoneNumber { get; set; }

        // Driver profile
        public string Address { get; set; }
        public DateTime DateOfBirth { get; set; }
        public Gender Gender { get; set; }
        public EmploymentStatus EmploymentStatus { get; set; }
        public string? LicenseNumber { get; set; }
        public DateTime? LicenseExpiryDate { get; set; }
        public long CompanyBranchId { get; set; }
        public LicenseCategory LicenseCategory { get; set; }
        public ShiftStatus ShiftStatus { get; set; }

        public IFormFile LicensePhoto { get; set; }
        public IFormFile PassportPhoto { get; set; }
    }
}

