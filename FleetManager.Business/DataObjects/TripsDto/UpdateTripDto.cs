using FleetManager.Business.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.TripsDto
{
    public class UpdateTripDto
    {
        [Required]
        public long Id { get; set; }

        [Required]
        public long VehicleId { get; set; }

        public long? DriverId { get; set; }

        [Required]
        [StringLength(500)]
        public string Origin { get; set; }

        [Required]
        [StringLength(500)]
        public string Destination { get; set; }

        [Required]
        [StringLength(500)]
        public string Purpose { get; set; }

        [StringLength(2000)]
        public string? Description { get; set; }

        [Required]
        public DateTime ScheduledStartDate { get; set; }

        [Required]
        public DateTime ScheduledEndDate { get; set; }

        public decimal? EstimatedDistance { get; set; }
        public decimal? EstimatedFuelCost { get; set; }

        [Required]
        public TripPriority Priority { get; set; }

        public bool RequiresApproval { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }
    }
}
