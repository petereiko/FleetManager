using FleetManager.Business.DataObjects.TripsDto;
using FleetManager.Business.Interfaces.TripLocationModule;
using FleetManager.Business.UtilityModels;
using FleetManager.Business.UtilityModels.RedisConfiguration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Implementations.TripLocationModule
{
    public class LocationFilterService : ILocationFilterService
    {
        private readonly IOptions<LocationTrackingSettings> _settings;
        private readonly ILogger<LocationFilterService> _logger;

        public LocationFilterService(
            IOptions<LocationTrackingSettings> settings,
            ILogger<LocationFilterService> logger)
        {
            _settings = settings;
            _logger = logger;
        }

        public async Task<(bool ShouldSave, string Reason)> ShouldSaveCheckpointAsync(
            long tripId,
            LocationUpdate current,
            LastSavedCheckpoint? lastSaved)
        {
            // ✅ ALWAYS save if no previous checkpoint
            if (lastSaved == null)
            {
                return (true, "First checkpoint");
            }

            var reasons = new List<string>();

            // ✅ 1. Time-based: Force save if too much time has passed
            var timeSinceLastSave = (current.Timestamp - lastSaved.Timestamp).TotalMinutes;
            if (timeSinceLastSave >= _settings.Value.MaxTimeMinutesWithoutCheckpoint)
            {
                reasons.Add($"Time elapsed: {Math.Round(timeSinceLastSave, 1)} min");
                return (true, string.Join(", ", reasons));
            }

            // ✅ 2. Distance-based: Calculate distance moved
            double distanceMeters = GeoUtils.HaversineDistanceMeters(
                lastSaved.Latitude,
                lastSaved.Longitude,
                current.Latitude,
                current.Longitude
            );

            // Sanity check (GeoUtils returns non-nullable double for non-null inputs)
            if (double.IsNaN(distanceMeters) || double.IsInfinity(distanceMeters))
            {
                _logger.LogWarning("Could not calculate distance for trip {TripId}", tripId);
                return (false, "Invalid coordinates");
            }

            // ✅ 3. Adaptive distance threshold based on speed
            var distanceThreshold = GetDistanceThresholdBySpeed(current.Speed);

            if (distanceMeters >= distanceThreshold)
            {
                reasons.Add($"Distance: {Math.Round(distanceMeters, 0)}m (threshold: {distanceThreshold}m)");
            }

            // ✅ 4. Direction change detection
            if (current.Heading.HasValue && lastSaved.Heading.HasValue)
            {
                var headingChange = Math.Abs((double)(current.Heading.Value - lastSaved.Heading.Value));

                // Normalize heading difference (handle 350° to 10° = 20° change, not 340°)
                if (headingChange > 180)
                    headingChange = 360 - headingChange;

                if (headingChange >= _settings.Value.MinDirectionChangeDegreesForCheckpoint)
                {
                    reasons.Add($"Direction change: {Math.Round(headingChange, 0)}°");
                }
            }

            // ✅ 5. Speed change detection
            if (current.Speed.HasValue && lastSaved.Speed.HasValue)
            {
                var speedChange = Math.Abs((double)(current.Speed.Value - lastSaved.Speed.Value));

                if (speedChange >= _settings.Value.MinSpeedChangeKmhForCheckpoint)
                {
                    reasons.Add($"Speed change: {Math.Round(speedChange, 1)} km/h");
                }
            }

            // ✅ 6. Stop detection
            if (current.Speed.HasValue && current.Speed.Value < _settings.Value.StopDetectionSpeedThresholdKmh)
            {
                // Check if this is a new stop (last checkpoint was moving)
                if (lastSaved.Speed.HasValue && lastSaved.Speed.Value >= _settings.Value.StopDetectionSpeedThresholdKmh)
                {
                    reasons.Add("Vehicle stopped");
                }
            }

            // ✅ 7. Resume from stop detection
            if (current.Speed.HasValue && current.Speed.Value >= _settings.Value.StopDetectionSpeedThresholdKmh)
            {
                if (lastSaved.Speed.HasValue && lastSaved.Speed.Value < _settings.Value.StopDetectionSpeedThresholdKmh)
                {
                    reasons.Add("Vehicle resumed");
                }
            }

            // Decision: Save if ANY significant change detected
            if (reasons.Any())
            {
                _logger.LogDebug(
                    "Checkpoint significant for trip {TripId}: {Reasons}",
                    tripId,
                    string.Join(", ", reasons)
                );
                return (true, string.Join(", ", reasons));
            }

            _logger.LogDebug(
                "Checkpoint skipped for trip {TripId}. Distance: {Distance}m, Time: {Time}s",
                tripId,
                Math.Round(distanceMeters, 0),
                Math.Round((current.Timestamp - lastSaved.Timestamp).TotalSeconds, 0)
            );

            return (false, "No significant change");
        }

        private int GetDistanceThresholdBySpeed(decimal? speed)
        {
            if (!speed.HasValue)
                return _settings.Value.MinDistanceMetersForCheckpoint;

            var speedKmh = (double)speed.Value;

            // Highway speed: save every 2km
            if (speedKmh >= _settings.Value.HighSpeedThresholdKmh)
                return _settings.Value.HighSpeedDistanceMeters;

            // City speed: save every 500m
            if (speedKmh >= _settings.Value.MediumSpeedThresholdKmh)
                return _settings.Value.MediumSpeedDistanceMeters;

            // Slow/traffic: save every 300m
            return _settings.Value.LowSpeedDistanceMeters;
        }
    }
}
