using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.ApiModels
{
    public class FineTollRequest
    {
        [Required]
        public long VehicleId { get; set; }

        [Required]
        public string Type { get; set; } = string.Empty; // "Fine" or "Toll"

        [Required]
        [StringLength(100)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; }

        [StringLength(3, MinimumLength = 3)]
        public string Currency { get; set; } = "NGN";

        [Required]
        [StringLength(200)]
        public string Reason { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Notes { get; set; }

        [Display(Name = "Driver Covered Fee?")]
        public bool IsMinimal { get; set; } = false;

        public List<IFormFile>? Attachments { get; set; }
    }
}
