using FleetManager.Business.Enums;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects
{
    public class DriverDto
    {
        public long Id { get; set; }
        public string? FullName { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }

        public string? Address { get; set; }
        public DateTime DateOfBirth { get; set; }
        public Gender Gender { get; set; }
        public EmploymentStatus EmploymentStatus { get; set; }
        public string? LicenseNumber { get; set; }
        public DateTime? LicenseExpiryDate { get; set; }
        public long? CompanyBranchId { get; set; }
        public LicenseCategory LicenseCategory { get; set; }
        public ShiftStatus ShiftStatus { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public List<DriverDocumentDto> DriverDocuments { get; set; } = new();
        public IFormFile? PassportFile { get; set; }
        public string? PassportFileName { get; set; }
    }

}
