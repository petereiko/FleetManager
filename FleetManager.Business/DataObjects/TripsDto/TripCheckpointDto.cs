using FleetManager.Business.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.TripsDto
{
    public class TripCheckpointDto
    {
        public long Id { get; set; }
        public long TripId { get; set; }
        public string Location { get; set; }
        public string? Description { get; set; }
        public DateTime CheckpointTime { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public CheckpointType CheckpointType { get; set; }
        public string CheckpointTypeDisplay { get; set; }
        public string? Notes { get; set; }
        public string? ReadableAddress { get; set; } // Human-readable address
    }
}
