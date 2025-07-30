using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Database.Entities
{
    public class PublicHoliday:BaseEntity
    {
        public DateTime Date { get; set; }
        public string LocalName { get; set; } = null!;
        public string CountryCode { get; set; } = null!;
    }

}
