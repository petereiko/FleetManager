using FleetManager.Business.DataObjects.Schedule;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.ViewModels.Schedule
{
    public class TimeOffListViewModel
    {
        public IEnumerable<TimeOffRequestDto> Requests { get; set; } = Enumerable.Empty<TimeOffRequestDto>();
    }
}
