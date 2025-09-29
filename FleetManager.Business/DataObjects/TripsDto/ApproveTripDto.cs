using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.TripsDto
{
    public class ApproveTripDto
    {
        [Required]
        public long TripId { get; set; }

        [Required]
        public bool IsApproved { get; set; }

        [StringLength(1000)]
        public string? Comments { get; set; }
    }
}
