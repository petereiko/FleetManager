using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.TripsDto
{
    public class CompleteTripDto
    {
        [Required]
        public long TripId { get; set; }

        [Required]
        public int EndOdometer { get; set; }

        public decimal? ActualFuelCost { get; set; }

        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }
    }
}
