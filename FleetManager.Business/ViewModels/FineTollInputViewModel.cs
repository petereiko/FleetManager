using FleetManager.Business.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.ViewModels
{
    public class FineTollInputViewModel
    {
        public long? Id { get; set; }

        [Required, StringLength(3, MinimumLength = 3)]
        public string Currency { get; set; } = "NGN";

        [Display(Name = "Date Logged")]
        public DateTime DateLogged { get; set; } = DateTime.Today;

        public FineTollRegisterViewModel Input { get; set; }

        
        public IEnumerable<SelectListItem> Vehicles { get; set; } = new List<SelectListItem>();
        
        public IEnumerable<SelectListItem> Types { get; set; } = new List<SelectListItem>();

        
    }

}
