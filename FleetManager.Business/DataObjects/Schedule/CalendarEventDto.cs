using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.Schedule
{
    public class CalendarEventDto
    {
        public string Title { get; set; } = "";
        public string Start { get; set; } = "";
        public string? End { get; set; }
        public string Color { get; set; } = "";
    }
}
