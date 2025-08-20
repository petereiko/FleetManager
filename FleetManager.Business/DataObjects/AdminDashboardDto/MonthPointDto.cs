using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.AdminDashboardDto
{
    public class MonthPointDto
    {
        public int Year { get; set; }
        public int Month { get; set; } // 1..12
        public decimal Value { get; set; } // money or liters
        public decimal SecondaryValue { get; set; } // optional (e.g. liters)
    }

}
