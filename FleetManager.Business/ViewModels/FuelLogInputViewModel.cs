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
    public class FuelLogInputViewModel
    {
        public long? Id { get; set; }

        [Required]
        [Display(Name = "Driver")]
        public long DriverId { get; set; }

        [Required]
        [Display(Name = "Vehicle")]
        public long VehicleId { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; }

        [Range(0, double.MaxValue)]
        [Display(Name = "Odometer Reading")]
        public int? Odometer { get; set; }

        [Required]
        [Range(0.01, double.MaxValue)]
        [Display(Name = "Volume (L)")]
        public decimal Volume { get; set; }

        [Required]
        [Range(0.01, double.MaxValue)]
        [Display(Name = "Cost")]
        [DataType(DataType.Currency)]
        public decimal Cost { get; set; }

        [Required]
        [Display(Name = "Fuel Type")]
        public FuelType FuelType { get; set; }

        [Display(Name = "Receipt (PDF/Image)")]
        public IFormFile? ReceiptFile { get; set; }

        public string? ExistingReceiptPath { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        // Dropdown lists
        public List<SelectListItem> Drivers { get; set; } = new();
        public List<SelectListItem> Vehicles { get; set; } = new();
        public List<SelectListItem> FuelTypes { get; set; } = new();
    }
}
