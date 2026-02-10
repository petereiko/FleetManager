using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.UtilityModels.RedisConfiguration
{
    // Business/UtilityModels/RedisConfiguration/LocationTrackingSettings.cs
    public class LocationTrackingSettings
    {
        // Redis throttling (still keep for API rate limiting)
        public int MinUpdateIntervalSeconds { get; set; } = 10; // Accept updates every 10s

        // Database persistence criteria (much more selective)
        public int MinDistanceMetersForCheckpoint { get; set; } = 500; // 500m minimum distance
        public int MaxTimeMinutesWithoutCheckpoint { get; set; } = 5; // Force save every 5 minutes
        public int MinDirectionChangeDegreesForCheckpoint { get; set; } = 30; // 30° direction change
        public int MinSpeedChangeKmhForCheckpoint { get; set; } = 20; // 20 km/h speed change

        // Adaptive distance based on speed
        public int HighSpeedThresholdKmh { get; set; } = 60; // Highway speed
        public int HighSpeedDistanceMeters { get; set; } = 2000; // Save every 2km on highway
        public int MediumSpeedThresholdKmh { get; set; } = 30; // City speed
        public int MediumSpeedDistanceMeters { get; set; } = 500; // Save every 500m in city
        public int LowSpeedDistanceMeters { get; set; } = 300; // Save every 300m in traffic

        // Redis settings
        public int LocationBufferSize { get; set; } = 50; // Process when queue reaches 50
        public int LocationExpiryHours { get; set; } = 24; // Keep in Redis for 24 hours
        public int BackgroundJobIntervalMinutes { get; set; } = 5; // Process queue every 5 minutes

        // Stop detection
        public int StopDetectionSpeedThresholdKmh { get; set; } = 5; // Consider stopped if <5 km/h
        public int StopDurationSecondsBeforeSave { get; set; } = 120; // Save stop after 2 minutes
    }
}
