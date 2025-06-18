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
    public class FineTollStatusUpdateViewModel
    {
        public long Id { get; set; }

        [Required]
        [Display(Name = "Status")]
        public FineTollStatus Status { get; set; }

        public IEnumerable<SelectListItem> StatusOptions { get; set; } = new List<SelectListItem>();
    }
}
