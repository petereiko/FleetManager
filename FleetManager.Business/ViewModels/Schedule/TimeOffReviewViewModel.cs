using FleetManager.Business.DataObjects.Schedule;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.ViewModels.Schedule
{
    public class TimeOffReviewViewModel
    {
        public TimeOffRequestDto Request { get; set; } = null!;

        [StringLength(500)]
        public string? AdminComment { get; set; }
    }
}
