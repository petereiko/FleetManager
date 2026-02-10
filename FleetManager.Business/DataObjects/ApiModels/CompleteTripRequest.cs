using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.ApiModels
{
    public class CompleteTripRequest
    {
        [Required]
        public long TripId { get; set; }

        [Required]
        [Range(0, int.MaxValue, ErrorMessage = "End odometer must be a positive number")]
        public int EndOdometer { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? ActualFuelCost { get; set; }

        [Range(-90, 90)]
        public decimal? Latitude { get; set; }

        [Range(-180, 180)]
        public decimal? Longitude { get; set; }

        [Range(0, 10000)]
        public decimal? LatitudeAccuracy { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }
    }
}
