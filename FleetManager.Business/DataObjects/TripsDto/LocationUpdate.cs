using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.TripsDto
{
    public class LocationUpdate
{
    public long TripId { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public decimal? Accuracy { get; set; }
    public decimal? Speed { get; set; } // km/h
    public decimal? Heading { get; set; } // degrees (0-360)
    public DateTime Timestamp { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string? DeviceId { get; set; }
    
    // ✅ Add these for intelligent filtering
    public bool IsSignificant { get; set; } // Marked by algorithm
    public string? SignificanceReason { get; set; } // Why this location is significant
}
}
