using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.TripsDto
{
    public class AssignTripDto
    {
        [Required]
        public long TripId { get; set; }

        [Required(ErrorMessage = "Driver is required")]
        public long DriverId { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }
    }
}
