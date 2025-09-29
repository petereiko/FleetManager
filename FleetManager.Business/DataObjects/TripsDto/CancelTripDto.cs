using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.TripsDto
{
    public class CancelTripDto
    {
        [Required]
        public long TripId { get; set; }

        [Required(ErrorMessage = "Cancellation reason is required")]
        [StringLength(1000)]
        public string CancellationReason { get; set; }
    }
}
