using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.TripReportsDto
{
    public class DailyTripSummaryDto
    {
        public DateTime Date { get; set; } // UTC midnight
        public int TotalTrips { get; set; }
        public int Scheduled { get; set; }
        public int Assigned { get; set; }
        public int InProgress { get; set; }
        public int Completed { get; set; }
        public int Cancelled { get; set; }
        public decimal TotalDistance { get; set; }
        public decimal TotalFuelCost { get; set; }
    }

}
