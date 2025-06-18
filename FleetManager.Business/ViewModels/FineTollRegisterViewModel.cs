using FleetManager.Business.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.ViewModels
{
    public class FineTollRegisterViewModel
    {
        [Required]
        [Display(Name = "Vehicle")]
        public long VehicleId { get; set; }

        [Required]
        [Display(Name = "Type")]
        public FineTollType Type { get; set; }

        [Required, StringLength(100)]
        public string Title { get; set; } = string.Empty;

        [Required, Range(0.01, double.MaxValue)]
        public decimal Amount { get; set; }

        

        [Required, StringLength(200)]
        public string Reason { get; set; } = string.Empty;

        [StringLength(500)]
        public string Notes { get; set; } = string.Empty;

        [Display(Name = "Driver Covered Fee?")]
        public bool IsMinimal { get; set; }

        
    }
}
