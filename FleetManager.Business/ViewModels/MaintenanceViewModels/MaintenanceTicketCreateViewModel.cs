using FleetManager.Business.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.ViewModels.MaintenanceViewModels
{
    public class MaintenanceTicketCreateViewModel
    {
        [Required]
        [Display(Name = "Driver")]
        public long DriverId { get; set; }

        [Required]
        [Display(Name = "Vehicle")]
        public long VehicleId { get; set; }

        public MaintenancePriority Priority { get; set; }

        [Required, StringLength(150)]
        public string Subject { get; set; }

        [Display(Name = "Notes / Description")]
        public string? Notes { get; set; }
        [Required]
        public List<SelectListItem> Priorities { get; set; } = new();

        /// <summary>
        /// The line‐items (parts) the driver is requesting work on.
        /// </summary>
        public List<MaintenanceTicketItemInputViewModel> Items { get; set; } = new();

        // Select‐lists to populate dropdowns
        public List<SelectListItem> Vehicles { get; set; } = new();
        public List<SelectListItem> Drivers { get; set; } = new();
        public List<SelectListItem> PartCategories { get; set; } = new();
    }
}
