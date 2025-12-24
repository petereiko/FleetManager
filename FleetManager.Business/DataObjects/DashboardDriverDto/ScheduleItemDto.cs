using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.DashboardDriverDto
{
    public class ScheduleItemDto
    {
        public long TripId { get; set; }
        public DateTime ScheduledStart { get; set; }
        public string TimeDisplay { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string ShortDescription { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}
