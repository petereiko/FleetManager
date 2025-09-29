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
       
        public TripDto Trip { get; set; }
        
        [Display(Name = "Driver")]
        public long? DriverId { get; set; }
        public List<SelectListItem> Drivers { get; set; } = new List<SelectListItem>();

        [StringLength(2000)]
        public string Notes { get; set; }
    }
}
