using FleetManager.Business.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Database.Entities
{
    public class TripCheckpoint : BaseEntity
    {
        public long TripId { get; set; }
        public virtual Trip Trip { get; set; }

        public string Location { get; set; }
        public string? Description { get; set; }
        public DateTime CheckpointTime { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }

        public CheckpointType CheckpointType { get; set; } // Start, Stop, Waypoint, End
        public string? Notes { get; set; }
    }
}
