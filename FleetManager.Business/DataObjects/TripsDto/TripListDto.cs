using FleetManager.Business.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.TripsDto
{
    public class TripListDto
    {
        public long Id { get; set; }
        public string TripNumber { get; set; }
        public string VehiclePlateNo { get; set; }
        public string? DriverName { get; set; }
        public string Origin { get; set; }
        public string Destination { get; set; }
        public DateTime ScheduledStartDate { get; set; }
        public DateTime ActualStartDate { get; set; }
        public decimal? EstimatedDistance { get; set; }
        public DateTime ScheduledEndDate { get; set; }
        public TripStatus Status { get; set; }
        public string StatusDisplay { get; set; }
        public TripPriority Priority { get; set; }
        public string PriorityDisplay { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool RequiresApproval { get; set; }
    }
}
