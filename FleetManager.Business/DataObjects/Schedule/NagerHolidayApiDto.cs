using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.Schedule
{
    public class NagerHolidayApiDto
    {
        public DateTime Date { get; set; }
        public string LocalName { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string CountryCode { get; set; } = null!;
        public bool Fixed { get; set; }
    }

}
