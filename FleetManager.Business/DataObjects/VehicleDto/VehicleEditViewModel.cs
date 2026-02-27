using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FleetManager.Business.Enums;

namespace FleetManager.Business.DataObjects.VehicleDto
{

    public class VehicleEditViewModel
    {
        

        [BindNever]
        public long? Id { get; set; }                     // set by controller after decryption; never in form

        // ── Classification
        [Required(ErrorMessage = "Please select a vehicle type.")]
        public VehicleType VehicleType { get; set; }

        // ── Standard vehicles (cars, trucks, vans …)
        public int? VehicleMakeId { get; set; }
        public int? VehicleModelId { get; set; }

        // ── Motorcycles 
        // Also validated conditionally.
        public string? CustomMakeName { get; set; }
        public string? CustomModelName { get; set; }

        // ── Core details 
        [Required(ErrorMessage = "Year is required.")]
        [Range(1900, 2100, ErrorMessage = "Enter a valid year.")]
        public int Year { get; set; }

        [Required(ErrorMessage = "VIN is required.")]
        public string VIN { get; set; } = string.Empty;

        [Required(ErrorMessage = "Plate number is required.")]
        public string PlateNo { get; set; } = string.Empty;

        [Required(ErrorMessage = "Color is required.")]
        public string Color { get; set; } = string.Empty;

        public string? EngineNumber { get; set; }
        public string? ChassisNumber { get; set; }

        public DateTime? RegistrationDate { get; set; }
        public DateTime? LastServiceDate { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Mileage must be a positive number.")]
        public int? Mileage { get; set; }

        // ── Nullable enums — CRITICAL ────────────────────────────────────────────
        // Using nullable enums here prevents MVC from throwing a binding error
        // when the user leaves the optional select on "— Select —" (empty string).
        // The controller / service layer converts null → default if needed.
        public FuelType? FuelType { get; set; }
        public TransmissionType? TransmissionType { get; set; }
        public VehicleStatus? VehicleStatus { get; set; }

        // ── Insurance / compliance
        public string? InsuranceCompany { get; set; }
        public DateTime? InsuranceExpiryDate { get; set; }
        public DateTime? RoadWorthyExpiryDate { get; set; }

        // ── Ownership 
        [Required(ErrorMessage = "Branch is required.")]
        public long? CompanyBranchId { get; set; }   // nullable so empty select doesn't bind to 0

        // ── Uploads (optional) 
        public List<IFormFile>? PhotoFiles { get; set; }
        public List<IFormFile>? DocumentFiles { get; set; }

        // ════════════════════════════════════════════════════════════════════════
        // EVERYTHING BELOW IS [BindNever]  ── these are NEVER in the POST body
        // ════════════════════════════════════════════════════════════════════════

        // ── Display-only strings (populated on GET, not posted back) ─────────────
        [BindNever] public string? Make { get; set; }
        [BindNever] public string? Model { get; set; }

        // ── Existing media (Edit page read-only lists) ───────────────────────────
        [BindNever] public List<VehicleDocumentDto> Photos { get; set; } = new();
        [BindNever] public List<VehicleDocumentDto> Documents { get; set; } = new();
        [BindNever] public List<VehicleDocumentDto> ExistingImages { get; set; } = new();
        [BindNever] public List<VehicleDocumentDto> ExistingDocuments { get; set; } = new();

        // ── Dropdown data (populated in PopulateSelectsAsync, never posted) ──────
        [BindNever] public IEnumerable<SelectListItem> Branches { get; set; } = Enumerable.Empty<SelectListItem>();
        [BindNever] public IEnumerable<SelectListItem> FuelTypes { get; set; } = Enumerable.Empty<SelectListItem>();
        [BindNever] public IEnumerable<SelectListItem> TransmissionTypes { get; set; } = Enumerable.Empty<SelectListItem>();
        [BindNever] public IEnumerable<SelectListItem> Statuses { get; set; } = Enumerable.Empty<SelectListItem>();
        [BindNever] public IEnumerable<SelectListItem> VehicleTypes { get; set; } = Enumerable.Empty<SelectListItem>();
        [BindNever] public IEnumerable<SelectListItem> Makes { get; set; } = Enumerable.Empty<SelectListItem>();
        [BindNever] public IEnumerable<SelectListItem> Models { get; set; } = Enumerable.Empty<SelectListItem>();
    }


    //public class VehicleEditViewModel : VehicleDto
    //{
    //    public VehicleEditViewModel() { }
    //    public VehicleEditViewModel(VehicleDto m)
    //    {
    //        // copy all properties
    //        foreach (var prop in typeof(VehicleDto).GetProperties())
    //            prop.SetValue(this, prop.GetValue(m));
    //        ExistingImages = m.ExistingImages;
    //        ExistingDocuments = m.ExistingDocuments;
    //    }

    //    public IEnumerable<SelectListItem> Branches { get; set; }
    //    public IEnumerable<SelectListItem> FuelTypes { get; set; }
    //    public IEnumerable<SelectListItem> TransmissionTypes { get; set; }
    //    public IEnumerable<SelectListItem> Statuses { get; set; }
    //    public IEnumerable<SelectListItem> VehicleTypes { get; set; }
    //    public IEnumerable<SelectListItem> Makes { get; set; }
    //    public IEnumerable<SelectListItem> Models { get; set; }
    //}
}
