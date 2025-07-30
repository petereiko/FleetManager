using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.Schedule
{
    public class PublicHolidayDto
    {
        public DateTime Date { get; set; }
        public string LocalName { get; set; } = null!;
        public string CountryCode { get; set; } = null!;
        public DateTime CreatedDate { get; set;}
        public DateTime Approved { get; set; }
    }

}
