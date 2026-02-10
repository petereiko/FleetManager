using FleetManager.Business.DataObjects.TripsDto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.ViewModels.TripsViewModels
{
    public class TripTrackingViewModel
    {
        public TripDto Trip { get; set; }
        public TripLocationDto? LatestLocation { get; set; }
        public bool IsActive { get; set; }
    }
}
