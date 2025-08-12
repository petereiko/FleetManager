using FleetManager.Business.DataObjects.Schedule;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.ViewModels.Schedule
{
    public class TimeOffDeniedListViewModel
    {
        public List<TimeOffRequestDto> DeniedRequests { get; set; } = new List<TimeOffRequestDto>();

        // Optional: Add summary statistics
        public int TotalApproved => DeniedRequests.Count;
        public int ActiveRequests => DeniedRequests.Count(r => r.EndDate >= DateTime.Now.Date);
        public int PastRequests => DeniedRequests.Count(r => r.EndDate < DateTime.Now.Date);
        public int UniqueDrivers => DeniedRequests.Select(r => r.RequestedBy).Distinct().Count();

        // Optional: Filter properties for future enhancements
        public string SearchTerm { get; set; } = string.Empty;
        public DateTime? StartDateFilter { get; set; }
        public DateTime? EndDateFilter { get; set; }
        public string DriverFilter { get; set; } = string.Empty;
    }
}
