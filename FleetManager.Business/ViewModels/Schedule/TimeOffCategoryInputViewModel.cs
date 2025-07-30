using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.ViewModels.Schedule
{
    public class TimeOffCategoryInputViewModel
    {
        public long? Id { get; set; }

        [Required, StringLength(100)]
        [Display(Name = "Category Name")]
        public string Name { get; set; } = null!;

        [StringLength(500)]
        [Display(Name = "Description (optional)")]
        public string? Description { get; set; }
    }
}
