using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.TripsDto
{
    public class StartTripDto
    {
        [Required]
        public long TripId { get; set; }

        [Required]
        public int StartOdometer { get; set; }

        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }
    }
}
