using FleetManager.Business.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.TripsDto
{
    public class CreateTripDto
    {
        [Required(ErrorMessage = "Vehicle is required")]
        public long VehicleId { get; set; }

        public long? DriverId { get; set; } // Optional at creation, can be assigned later

        [Required(ErrorMessage = "Origin is required")]
        [StringLength(500)]
        public string Origin { get; set; }

        [Required(ErrorMessage = "Destination is required")]
        [StringLength(500)]
        public string Destination { get; set; }

        [Required(ErrorMessage = "Purpose is required")]
        [StringLength(500)]
        public string Purpose { get; set; }

        [StringLength(2000)]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Scheduled start date is required")]
        public DateTime ScheduledStartDate { get; set; }

        [Required(ErrorMessage = "Scheduled end date is required")]
        public DateTime ScheduledEndDate { get; set; }

        public decimal? EstimatedDistance { get; set; }
        public decimal? EstimatedFuelCost { get; set; }

        [Required]
        public TripPriority Priority { get; set; } = TripPriority.Normal;

        public bool RequiresApproval { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }
    }
}
