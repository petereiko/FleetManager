using FleetManager.Business.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.TripsDto
{
    public class TripFilterDto
    {
        public string? SearchTerm { get; set; }
        public TripStatus? Status { get; set; }
        public TripPriority? Priority { get; set; }
        public long? DriverId { get; set; }
        public long? VehicleId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
