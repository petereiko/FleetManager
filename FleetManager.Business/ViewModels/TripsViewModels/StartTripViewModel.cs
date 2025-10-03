using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.ViewModels.TripsViewModels
{
    public class StartTripViewModel
    {
        public long TripId { get; set; }

        [Display(Name = "Start Odometer (km)")]
        [Required]
        public int StartOdometer { get; set; }

        // client-side filled
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public decimal? LatitudeAccuracy { get; set; }

        

        [DataType(DataType.MultilineText)]
        public string? Notes { get; set; }

        public int? PreferredStartOdometer { get; set; }   // value to prefill StartOdometer input
        public int? CurrentVehicleMileage { get; set; }    // display-only hint
    }


    public class CompleteTripViewModel
    {
        public long TripId { get; set; }

        [Display(Name = "End Odometer (km)")]
        [Required]
        public int EndOdometer { get; set; }

        [Display(Name = "Actual Fuel Cost")]
        public decimal? ActualFuelCost { get; set; }

        // client-side filled
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public decimal? LatitudeAccuracy { get; set; }

        [DataType(DataType.MultilineText)]
        public string? Notes { get; set; }

        public int? PreferredEndOdometer { get; set; }   // value to prefill StartOdometer input
        public int? CurrentVehicleMileage { get; set; }    // display-only hint
    }
}

