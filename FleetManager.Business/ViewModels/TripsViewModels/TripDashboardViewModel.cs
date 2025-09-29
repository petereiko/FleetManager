using FleetManager.Business.DataObjects.TripsDto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.ViewModels.TripsViewModels
{
    public class TripDashboardViewModel
    {
        public TripStatistics Statistics { get; set; }
        public List<TripListDto> UpcomingTrips { get; set; }
        public List<TripListDto> ActiveTrips { get; set; }
        public List<TripListDto> PendingApprovalTrips { get; set; }
    }
}
