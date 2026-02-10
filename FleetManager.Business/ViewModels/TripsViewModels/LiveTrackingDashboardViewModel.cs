using FleetManager.Business.DataObjects.TripsDto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.ViewModels.TripsViewModels
{
    public class LiveTrackingDashboardViewModel
    {
        public List<TripListDto> ActiveTrips { get; set; } = new();
        public int TotalActiveTrips { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}
