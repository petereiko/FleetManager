using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.ViewModels
{
    public class AssignmentCreateViewModel
    {
       public AssignmentInputModel Input { get; set; } = new();

        public IEnumerable<SelectListItem> Drivers { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> Vehicles { get; set; } = new List<SelectListItem>();
    }
}
