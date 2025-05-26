using FleetManager.Business.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.ViewModels
{
    public class DriverOnboardViewModel
    {
        public DriverOnboardInputModel Input { get; set; } = new();



        /// <summary>
        /// Populated server‐side with all branches for dropdown.
        /// </summary>
        public IEnumerable<SelectListItem> Branches { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> Genders { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> EmploymentStatuses { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> ShiftStatuses { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> LicenseCategories { get; set; } = new List<SelectListItem>();
    }

}
