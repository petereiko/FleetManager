using FleetManager.Business.DataObjects.Schedule;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.ViewModels.Schedule
{
    public class DriverTimeOffIndexViewModel
    {
        public List<SelectListItem> Categories { get; set; } = new();
        public List<TimeOffRequestDto> Requests { get; set; } = new();
    }
}
