using FleetManager.Business.DataObjects.Schedule;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.ViewModels.Schedule
{
    public class TimeOffPendingListViewModel
    {
        public IEnumerable<TimeOffRequestDto> PendingRequests { get; set; } = Enumerable.Empty<TimeOffRequestDto>();
    }
}
