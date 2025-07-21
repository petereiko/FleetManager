using FleetManager.Business.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.ViewModels.MaintenanceViewModels
{
    public class MaintenanceTicketItemInputViewModel
    {
        [Required]
        [Display(Name = "Part Category")]
        public int PartCategoryId { get; set; }

        [Required]
        [Display(Name = "Part")]
        public int PartId { get; set; }

        [Display(Name = "Description (if custom)")]
        public string? CustomDescription { get; set; }

        [Required]
        [Range(1, 1000)]
        public int Quantity { get; set; }
        [Required]
        public MaintenancePriority? Priority { get; set; }

        [Required]
        [Range(0.0, 100000.0)]
        [DataType(DataType.Currency)]
        public decimal UnitPrice { get; set; }
    }
}
