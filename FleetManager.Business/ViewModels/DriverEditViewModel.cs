using FleetManager.Business.DataObjects;
using FleetManager.Business.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.ViewModels
{
    public class DriverEditViewModel
    {
        [Required]
        public long Id { get; set; }

        [Required, StringLength(50)]
        public string FirstName { get; set; }

        [Required, StringLength(50)]
        public string LastName { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        [Required, Phone]
        public string PhoneNumber { get; set; }

        [Required, StringLength(200)]
        public string Address { get; set; }

        [Required, DataType(DataType.Date)]
        public DateTime DateOfBirth { get; set; }

        [Required]
        public Gender Gender { get; set; }

        [Required]
        public EmploymentStatus EmploymentStatus { get; set; }

        [StringLength(20)]
        public string? LicenseNumber { get; set; }

        [DataType(DataType.Date)]
        public DateTime? LicenseExpiryDate { get; set; }

        [Required]
        [Display(Name = "Branch")]
        public long CompanyBranchId { get; set; }

        [Required, StringLength(5)]
        public LicenseCategory LicenseCategory { get; set; }

        [Required]
        public ShiftStatus ShiftStatus { get; set; }

        [Display(Name = "Active?")]
        public bool IsActive { get; set; }

        // New file‐upload properties:
        [Display(Name = "Driver's License Photo")]
        public IFormFile LicensePhoto { get; set; }

        [Display(Name = "Profile Photo")]
        public IFormFile ProfilePhoto { get; set; }

        // Existing photos for display
        public List<DriverDocumentDto> ExistingLicensePhotos { get; set; } = new();
        public List<DriverDocumentDto> ExistingProfilePhotos {  get; set; } = new();
       

        /// <summary>
        /// Populated server‐side with all branches for dropdown.
        /// </summary>
        public IEnumerable<SelectListItem> Branches { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> Genders { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> EmploymentStatuses { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> ShiftStatuses { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> LicenseCategories { get; set; } = new List<SelectListItem>();


    }
}
