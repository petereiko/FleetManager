using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.ApiModels
{
    public class FuelLogRequest
    {
        [Required]
        public long VehicleId { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Odometer must be a positive number")]
        public int? Odometer { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Volume must be greater than 0")]
        public decimal Volume { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Cost must be greater than 0")]
        public decimal Cost { get; set; }

        [Required]
        public string FuelType { get; set; } = string.Empty; // "Petrol", "Diesel", "Electric", "Hybrid"

        [StringLength(500)]
        public string? Notes { get; set; }

        // For file upload - will be handled separately in multipart/form-data
        // Not in JSON body
    }
}
