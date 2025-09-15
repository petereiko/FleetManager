using FleetManager.Business.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.ViewModels.RepairHistoryViewModels
{
    public class RepairCreateViewModel
    {
        // Dropdown selections
        [Required]
        [Display(Name = "Vehicle")]
        public long VehicleId { get; set; }

        [Display(Name = "Driver (optional)")]
        public long? DriverId { get; set; }

        [Required]
        [StringLength(200)]
        public string Subject { get; set; } = string.Empty;

        public string? Notes { get; set; }

        [Display(Name = "Priority")]
        public MaintenancePriority Priority { get; set; } = MaintenancePriority.Moderate;

        // Line items (at least one row in UI)
        public List<RepairItemInputViewModel> Items { get; set; } = new List<RepairItemInputViewModel>
        {
            new RepairItemInputViewModel()
        };

        // Helper dropdown collections (populated by controller)
        public List<SelectListItem> Drivers { get; set; } = new();
        public List<SelectListItem> Vehicles { get; set; } = new();
        public List<SelectListItem> PartCategories { get; set; } = new();
        public List<SelectListItem> Priorities { get; set; } = new();
    }
}
