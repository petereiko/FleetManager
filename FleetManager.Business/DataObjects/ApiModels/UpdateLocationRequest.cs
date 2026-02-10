using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.ApiModels
{
    public class UpdateLocationRequest
    {
        [Required]
        public long TripId { get; set; }

        [Required]
        [Range(-90, 90)]
        public decimal Latitude { get; set; }

        [Required]
        [Range(-180, 180)]
        public decimal Longitude { get; set; }

        [Range(0, 10000)]
        public decimal? Accuracy { get; set; }

        public decimal? Speed { get; set; } // km/h

        public decimal? Heading { get; set; } // degrees

        [StringLength(100)]
        public string? DeviceId { get; set; } // ✅ Add this
    }
}
