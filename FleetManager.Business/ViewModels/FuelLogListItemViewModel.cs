using FleetManager.Business.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.ViewModels
{
    public class FuelLogListItemViewModel
    {
        public long? Id { get; set; }

        [Display(Name = "Date")]
        public DateTime Date { get; set; }

        [Display(Name = "Driver")]
        public string DriverName { get; set; } = "";

        [Display(Name = "Vehicle")]
        public string VehicleDescription { get; set; } = "";

        [Display(Name = "License Number")]
        public string PlateNo { get; set; } = "";

        [Display(Name = "Odometer")]
        public int? Odometer { get; set; }

        [Display(Name = "Volume (L)")]
        public decimal Volume { get; set; }

        [Display(Name = "Cost")]
        public decimal Cost { get; set; }

        [Display(Name = "Fuel Type")]
        public string FuelType { get; set; }
    }
}
