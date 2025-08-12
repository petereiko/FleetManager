using FleetManager.Business.DataObjects.Schedule;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.ViewModels.Schedule
{
    public class TimeOffApprovedListViewModel
    {
        public List<TimeOffRequestDto> ApprovedRequests { get; set; } = new List<TimeOffRequestDto>();

        // Optional: Add summary statistics
        public int TotalApproved => ApprovedRequests.Count;
        public int ActiveRequests => ApprovedRequests.Count(r => r.EndDate >= DateTime.Now.Date);
        public int PastRequests => ApprovedRequests.Count(r => r.EndDate < DateTime.Now.Date);
        public int UniqueDrivers => ApprovedRequests.Select(r => r.RequestedBy).Distinct().Count();

        // Optional: Filter properties for future enhancements
        public string SearchTerm { get; set; } = string.Empty;
        public DateTime? StartDateFilter { get; set; }
        public DateTime? EndDateFilter { get; set; }
        public string DriverFilter { get; set; } = string.Empty;
    }
}
