using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.ApiModels
{
    public class StartTripRequest
    {
        [Required]
        public long TripId { get; set; }

        [Required]
        [Range(0, int.MaxValue, ErrorMessage = "Start odometer must be a positive number")]
        public int StartOdometer { get; set; }

        [Range(-90, 90)]
        public decimal? Latitude { get; set; }

        [Range(-180, 180)]
        public decimal? Longitude { get; set; }

        [Range(0, 10000)]
        public decimal? LatitudeAccuracy { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }
    }
}
