using FleetManager.Business.DataObjects.TripsDto;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.ViewModels.TripsViewModels
{
    public class AssignTripViewModel
    {
        public long TripId { get; set; }
        public string TripNumber { get; set; }
        public TripDto Trip { get; set; }
        
        [Display(Name = "Driver")]
        public long? DriverId { get; set; }
        public IEnumerable<SelectListItem> Drivers { get; set; } = Enumerable.Empty<SelectListItem>();

        [StringLength(2000)]
        [DataType(DataType.MultilineText)]
        public string Notes { get; set; }
    }
}
