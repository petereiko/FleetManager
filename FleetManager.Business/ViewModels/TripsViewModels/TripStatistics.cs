using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.ViewModels.TripsViewModels
{
    public class TripStatistics
    {
        public int TotalTrips { get; set; }
        public int ScheduledTrips { get; set; }
        public int ActiveTrips { get; set; }
        public int CompletedTrips { get; set; }
        public int CancelledTrips { get; set; }
        public int PendingApprovalTrips { get; set; }

        public decimal TotalDistanceCovered { get; set; }
        public decimal TotalFuelCost { get; set; }
        public decimal AverageTripDistance { get; set; }
        public decimal AverageTripCost { get; set; }

        public int TripsThisWeek { get; set; }
        public int TripsThisMonth { get; set; }
    }
}
