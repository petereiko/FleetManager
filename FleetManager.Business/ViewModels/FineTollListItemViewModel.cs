using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.ViewModels
{
    public class FineTollListItemViewModel
    {
        public long Id { get; set; }

        [Display(Name = "Date Logged")]
        public DateTime DateLogged { get; set; }

        [Display(Name = "Driver")]
        public string DriverName { get; set; } = string.Empty;

        [Display(Name = "Type")]
        public string Type { get; set; } = string.Empty;

        [Display(Name = "Title")]
        public string Title { get; set; } = string.Empty;

        [Display(Name = "Amount")]
        public decimal Amount { get; set; }

        [Display(Name = "Currency")]
        public string Currency { get; set; } = "NGN";

        [Display(Name = "Status")]
        public string Status { get; set; } = string.Empty;

        [Display(Name = "Vehicle")]
        public string VehicleDescription { get; set; } = string.Empty;

        [Display(Name = "Driver Covered")]
        public bool IsMinimal { get; set; }

        public IEnumerable<SelectListItem> StatusOptions { get; set; } = new List<SelectListItem>();

    }
}
