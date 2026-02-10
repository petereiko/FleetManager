using FleetManager.Business.Database.Entities;
using FleetManager.Business.DataObjects.TripsDto;
using FleetManager.Business.Enums;
using FleetManager.Business.Hubs;
using FleetManager.Business.Interfaces.TripLocationModule;
using FleetManager.Business.UtilityModels;
using FleetManager.Business.UtilityModels.RedisConfiguration;
using Hangfire;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Implementations.TripLocationModule
{

    public class TripLocationService : ITripLocationService
    {
        private readonly FleetManagerDbContext _context;
        private readonly IRedisService _redis;
        private readonly ILogger<TripLocationService> _logger;
        private readonly IOptions<LocationTrackingSettings> _settings;
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly ILocationFilterService _locationFilterService;
        private readonly IHubContext<TripTrackingHub> _hubContext;

        // Redis key patterns
        private const string LOCATION_QUEUE_KEY = "location:queue";
        private const string LAST_UPDATE_KEY = "location:last:{0}"; // {tripId}
        private const string TRIP_LOCATIONS_KEY = "location:trip:{0}"; // {tripId}
        private const string ACTIVE_TRIPS_KEY = "trips:active";

        public TripLocationService(
            FleetManagerDbContext context,
            IRedisService redis,
            ILogger<TripLocationService> logger,
            IOptions<LocationTrackingSettings> settings,
            IBackgroundJobClient backgroundJobClient,
            ILocationFilterService locationFilterService,
            IHubContext<TripTrackingHub> hubContext)
        {
            _context = context;
            _redis = redis;
            _logger = logger;
            _settings = settings;
            _backgroundJobClient = backgroundJobClient;
            _locationFilterService = locationFilterService;
            _hubContext = hubContext;
        }

        public async Task<MessageResponse> UpdateTripLocationAsync(LocationUpdate update)
        {
            try
            {
                var lastUpdateKey = string.Format(LAST_UPDATE_KEY, update.TripId);

                // ✅ 1. Check throttle using Redis (prevents DB hit)
                var lastUpdate = await _redis.GetAsync<DateTime?>(lastUpdateKey);
                var now = DateTime.UtcNow;

                if (lastUpdate.HasValue)
                {
                    var secondsSinceLastUpdate = (now - lastUpdate.Value).TotalSeconds;
                    if (secondsSinceLastUpdate < _settings.Value.MinUpdateIntervalSeconds)
                    {
                        _logger.LogDebug(
                            "Location update throttled for trip {TripId}. Last update: {Seconds}s ago",
                            update.TripId,
                            Math.Round(secondsSinceLastUpdate, 1)
                        );

                        return new MessageResponse
                        {
                            Success = true,
                            Message = $"Update throttled. Next update in {_settings.Value.MinUpdateIntervalSeconds - (int)secondsSinceLastUpdate}s"
                        };
                    }
                }

                // ✅ 2. Check if trip is active (cached in Redis)
                var activeTripIds = await _redis.SetMembersAsync<long>(ACTIVE_TRIPS_KEY);
                if (!activeTripIds.Contains(update.TripId))
                {
                    // Verify in DB and cache if active
                    var isActive = await _context.Trips
                        .AnyAsync(t => t.Id == update.TripId && t.Status == TripStatus.InProgress);

                    if (!isActive)
                    {
                        return new MessageResponse
                        {
                            Success = false,
                            Message = "Trip is not in progress"
                        };
                    }

                    // Add to active trips set
                    await _redis.SetAddAsync(ACTIVE_TRIPS_KEY, update.TripId);
                }

                // ✅ 3. Add to Redis queue for background processing
                update.Timestamp = now;
                await _redis.ListPushAsync(LOCATION_QUEUE_KEY, update);

                // ✅ 4. Store latest location for immediate retrieval
                var latestKey = string.Format(TRIP_LOCATIONS_KEY, update.TripId) + ":latest";
                await _redis.SetAsync(latestKey, update, TimeSpan.FromHours(_settings.Value.LocationExpiryHours));

                // ✅ 5. Update last update timestamp
                await _redis.SetAsync(lastUpdateKey, now, TimeSpan.FromMinutes(10));

                // ✅ 6. Store in trip location history (limited list for real-time tracking)
                var historyKey = string.Format(TRIP_LOCATIONS_KEY, update.TripId);
                var locationDto = new TripLocationDto
                {
                    TripId = update.TripId,
                    Latitude = update.Latitude,
                    Longitude = update.Longitude,
                    Accuracy = update.Accuracy,
                    Speed = update.Speed,
                    Heading = update.Heading,
                    Timestamp = update.Timestamp
                };

                await _redis.ListPushAsync(historyKey, locationDto);

                // Keep only last 100 locations in Redis (for real-time map display)
                await _redis.ListTrimAsync(historyKey, -100, -1);
                await _redis.KeyExpireAsync(historyKey, TimeSpan.FromHours(_settings.Value.LocationExpiryHours));

                // ✅ 7. **BROADCAST TO SIGNALR CLIENTS** - Real-time update
                await BroadcastLocationUpdate(update.TripId, locationDto);

                // ✅ 8. Check queue size and trigger background job if threshold reached
                var queueSize = await _redis.ListLengthAsync(LOCATION_QUEUE_KEY);
                if (queueSize >= _settings.Value.LocationBufferSize)
                {
                    _backgroundJobClient.Enqueue<ITripLocationService>(s => s.ProcessLocationQueueAsync());
                    _logger.LogInformation("Triggered location queue processing. Queue size: {QueueSize}", queueSize);
                }

                _logger.LogDebug(
                    "Location queued for trip {TripId}. Queue size: {QueueSize}",
                    update.TripId,
                    queueSize
                );

                return new MessageResponse
                {
                    Success = true,
                    Message = "Location updated successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating location for trip {TripId}", update.TripId);
                return new MessageResponse
                {
                    Success = false,
                    Message = "Error updating location"
                };
            }
        }

        /// <summary>
        /// Broadcast location update to all connected SignalR clients tracking this trip
        /// </summary>
        private async Task BroadcastLocationUpdate(long tripId, TripLocationDto location)
        {
            try
            {
                // Get trip info for richer update
                var tripInfo = await _context.Trips
                    .AsNoTracking()
                    .Where(t => t.Id == tripId)
                    .Select(t => new
                    {
                        t.TripNumber,
                        t.Status,
                        VehiclePlateNo = t.Vehicle.PlateNo,
                        DriverName = t.Driver.User.FirstName + " " + t.Driver.User.LastName
                    })
                    .FirstOrDefaultAsync();

                var updatePayload = new
                {
                    tripId = tripId,
                    location = new
                    {
                        latitude = location.Latitude,
                        longitude = location.Longitude,
                        accuracy = location.Accuracy,
                        speed = location.Speed,
                        heading = location.Heading,
                        timestamp = location.Timestamp
                    },
                    tripInfo = tripInfo,
                    updateTime = DateTime.UtcNow
                };

                // Broadcast to specific trip group
                await _hubContext.Clients
                    .Group($"trip-{tripId}")
                    .SendAsync("LocationUpdate", updatePayload);

                _logger.LogDebug(
                    "Broadcasted location update for trip {TripId} to SignalR clients",
                    tripId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting location update for trip {TripId}", tripId);
                // Don't throw - location is still saved, just broadcast failed
            }
        }

        public async Task ProcessLocationQueueAsync()
        {
            try
            {
                _logger.LogInformation("Starting location queue processing...");

                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                var processedCount = 0;
                var savedCount = 0;
                var skippedCount = 0;

                const int batchSize = 50;
                var queueSize = await _redis.ListLengthAsync(LOCATION_QUEUE_KEY);

                if (queueSize == 0)
                {
                    _logger.LogInformation("Location queue is empty");
                    return;
                }

                _logger.LogInformation("Processing {QueueSize} location updates...", queueSize);

                while (queueSize > 0)
                {
                    var batch = await _redis.ListRangeAsync<LocationUpdate>(LOCATION_QUEUE_KEY, 0, batchSize - 1);

                    if (!batch.Any()) break;

                    // Group by TripId for efficient processing
                    var groupedByTrip = batch.GroupBy(l => l.TripId);

                    using var transaction = await _context.Database.BeginTransactionAsync();
                    try
                    {
                        foreach (var tripGroup in groupedByTrip)
                        {
                            var tripId = tripGroup.Key;

                            // Get trip with vehicle
                            var trip = await _context.Trips
                                .Include(t => t.Vehicle)
                                .FirstOrDefaultAsync(t => t.Id == tripId);

                            if (trip == null || trip.Status != TripStatus.InProgress)
                            {
                                _logger.LogWarning("Trip {TripId} not found or not in progress. Skipping locations.", tripId);
                                skippedCount += tripGroup.Count();
                                continue;
                            }

                            // ✅ Get last saved checkpoint from Redis
                            var lastSavedKey = $"checkpoint:last:{tripId}";
                            var lastSaved = await _redis.GetAsync<LastSavedCheckpoint>(lastSavedKey);

                            // If not in Redis, get from database
                            if (lastSaved == null)
                            {
                                var lastCheckpoint = await _context.TripCheckpoints
                                    .Where(c => c.TripId == tripId && c.CheckpointType == CheckpointType.Waypoint)
                                    .OrderByDescending(c => c.CheckpointTime)
                                    .FirstOrDefaultAsync();

                                if (lastCheckpoint != null && lastCheckpoint.Latitude.HasValue && lastCheckpoint.Longitude.HasValue)
                                {
                                    lastSaved = new LastSavedCheckpoint
                                    {
                                        Latitude = lastCheckpoint.Latitude.Value,
                                        Longitude = lastCheckpoint.Longitude.Value,
                                        Speed = null,
                                        Heading = null,
                                        Timestamp = lastCheckpoint.CheckpointTime
                                    };
                                }
                            }

                            // Process each location update
                            foreach (var location in tripGroup.OrderBy(l => l.Timestamp))
                            {
                                try
                                {
                                    processedCount++;

                                    // ✅ Apply smart filtering
                                    var (shouldSave, reason) = await _locationFilterService.ShouldSaveCheckpointAsync(
                                        tripId,
                                        location,
                                        lastSaved
                                    );

                                    if (!shouldSave)
                                    {
                                        skippedCount++;
                                        _logger.LogDebug(
                                            "Skipped checkpoint for trip {TripId}: {Reason}",
                                            tripId,
                                            reason
                                        );
                                        continue;
                                    }

                                    // ✅ Save significant checkpoint
                                    var checkpoint = new TripCheckpoint
                                    {
                                        TripId = tripId,
                                        Location = $"GPS Update",
                                        Description = $"{location.Latitude:F6}, {location.Longitude:F6}",
                                        CheckpointTime = location.Timestamp,
                                        CheckpointType = CheckpointType.Waypoint,
                                        Latitude = location.Latitude,
                                        Longitude = location.Longitude,
                                        Notes = BuildCheckpointNotes(location, reason),
                                        IsActive = true,
                                        CreatedDate = DateTime.UtcNow,
                                        CreatedBy = location.UserId
                                    };

                                    _context.TripCheckpoints.Add(checkpoint);
                                    savedCount++;

                                    // ✅ Update vehicle location
                                    if (trip.Vehicle != null)
                                    {
                                        UpdateVehicleLocation(trip.Vehicle, location);
                                    }

                                    // ✅ Update last saved checkpoint in Redis
                                    lastSaved = new LastSavedCheckpoint
                                    {
                                        Latitude = location.Latitude,
                                        Longitude = location.Longitude,
                                        Speed = location.Speed,
                                        Heading = location.Heading,
                                        Timestamp = location.Timestamp
                                    };

                                    await _redis.SetAsync(
                                        lastSavedKey,
                                        lastSaved,
                                        TimeSpan.FromHours(_settings.Value.LocationExpiryHours)
                                    );

                                    _logger.LogInformation(
                                        "Saved checkpoint for trip {TripId}: {Reason}",
                                        tripId,
                                        reason
                                    );
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, "Error processing location for trip {TripId}", tripId);
                                }
                            }
                        }

                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();

                        // Remove processed items from queue
                        await _redis.ListTrimAsync(LOCATION_QUEUE_KEY, batch.Count, -1);

                        queueSize = await _redis.ListLengthAsync(LOCATION_QUEUE_KEY);
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        _logger.LogError(ex, "Error saving location batch to database");
                        break;
                    }
                }

                stopwatch.Stop();

                _logger.LogInformation(
                    "Location queue processing completed. Processed: {Processed}, Saved: {Saved}, Skipped: {Skipped}, Duration: {Duration}ms, Reduction: {Reduction}%",
                    processedCount,
                    savedCount,
                    skippedCount,
                    stopwatch.ElapsedMilliseconds,
                    processedCount > 0 ? Math.Round((skippedCount / (double)processedCount) * 100, 1) : 0
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fatal error in location queue processing");
            }
        }

        public async Task<List<TripLocationDto>> GetTripLocationsAsync(long tripId)
        {
            try
            {
                // ✅ Try Redis first
                var cacheKey = string.Format(TRIP_LOCATIONS_KEY, tripId);
                var cachedLocations = await _redis.ListRangeAsync<TripLocationDto>(cacheKey);

                if (cachedLocations.Any())
                {
                    _logger.LogDebug("Retrieved {Count} locations from Redis for trip {TripId}",
                        cachedLocations.Count, tripId);
                    return cachedLocations;
                }

                // ✅ Fallback to database
                var dbLocations = await _context.TripCheckpoints
                    .AsNoTracking()
                    .Where(c => c.TripId == tripId && c.Latitude.HasValue && c.Longitude.HasValue)
                    .OrderBy(c => c.CheckpointTime)
                    .Select(c => new TripLocationDto
                    {
                        TripId = c.TripId,
                        Latitude = c.Latitude.Value,
                        Longitude = c.Longitude.Value,
                        Timestamp = c.CheckpointTime
                    })
                    .ToListAsync();

                // ✅ Cache in Redis for future requests
                if (dbLocations.Any())
                {
                    foreach (var loc in dbLocations.TakeLast(100)) // Last 100 locations
                    {
                        await _redis.ListPushAsync(cacheKey, loc);
                    }
                    await _redis.KeyExpireAsync(cacheKey, TimeSpan.FromHours(_settings.Value.LocationExpiryHours));
                }

                return dbLocations;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting locations for trip {TripId}", tripId);
                return new List<TripLocationDto>();
            }
        }

        public async Task<TripLocationDto?> GetLatestLocationAsync(long tripId)
        {
            try
            {
                var latestKey = string.Format(TRIP_LOCATIONS_KEY, tripId) + ":latest";
                var cached = await _redis.GetAsync<LocationUpdate>(latestKey);

                if (cached != null)
                {
                    return new TripLocationDto
                    {
                        TripId = cached.TripId,
                        Latitude = cached.Latitude,
                        Longitude = cached.Longitude,
                        Accuracy = cached.Accuracy,
                        Speed = cached.Speed,
                        Heading = cached.Heading,
                        Timestamp = cached.Timestamp
                    };
                }

                // Fallback to DB
                var latest = await _context.TripCheckpoints
                    .AsNoTracking()
                    .Where(c => c.TripId == tripId && c.Latitude.HasValue && c.Longitude.HasValue)
                    .OrderByDescending(c => c.CheckpointTime)
                    .Select(c => new TripLocationDto
                    {
                        TripId = c.TripId,
                        Latitude = c.Latitude.Value,
                        Longitude = c.Longitude.Value,
                        Timestamp = c.CheckpointTime
                    })
                    .FirstOrDefaultAsync();

                return latest;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting latest location for trip {TripId}", tripId);
                return null;
            }
        }

        public async Task<MessageResponse> InvalidateTripCacheAsync(long tripId)
        {
            try
            {
                var keys = new[]
                {
                    string.Format(LAST_UPDATE_KEY, tripId),
                    string.Format(TRIP_LOCATIONS_KEY, tripId),
                    string.Format(TRIP_LOCATIONS_KEY, tripId) + ":latest"
                };

                foreach (var key in keys)
                {
                    await _redis.DeleteAsync(key);
                }

                // Remove from active trips if completed/cancelled
                var trip = await _context.Trips.FindAsync(tripId);
                if (trip != null && trip.Status != TripStatus.InProgress)
                {
                    await _redis.SetRemoveAsync(ACTIVE_TRIPS_KEY, tripId);
                }

                _logger.LogInformation("Cache invalidated for trip {TripId}", tripId);

                return new MessageResponse { Success = true, Message = "Cache cleared" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error invalidating cache for trip {TripId}", tripId);
                return new MessageResponse { Success = false, Message = "Error clearing cache" };
            }
        }

        #region Helper Methods

        private string BuildCheckpointNotes(LocationUpdate location, string significanceReason)
        {
            var notes = new List<string>();

            // Add significance reason
            notes.Add($"Significance: {significanceReason}");

            if (location.Accuracy.HasValue)
            {
                notes.Add($"Accuracy: ±{Math.Round((double)location.Accuracy.Value, 1)}m");
            }

            if (location.Speed.HasValue)
            {
                notes.Add($"Speed: {Math.Round((double)location.Speed.Value, 1)} km/h");
            }

            if (location.Heading.HasValue)
            {
                notes.Add($"Heading: {Math.Round((double)location.Heading.Value, 0)}°");
            }

            if (!string.IsNullOrEmpty(location.DeviceId))
            {
                notes.Add($"Device: {location.DeviceId}");
            }

            return string.Join(" | ", notes);
        }

        private void UpdateVehicleLocation(Vehicle vehicle, LocationUpdate location)
        {
            var vehicleType = vehicle.GetType();
            var latProp = vehicleType.GetProperty("LastKnownLatitude");
            var lonProp = vehicleType.GetProperty("LastKnownLongitude");
            var updateProp = vehicleType.GetProperty("LastLocationUpdate");

            if (latProp != null && lonProp != null)
            {
                latProp.SetValue(vehicle, location.Latitude);
                lonProp.SetValue(vehicle, location.Longitude);
                updateProp?.SetValue(vehicle, location.Timestamp);
            }
        }

        #endregion
    }







    //public class TripLocationService : ITripLocationService
    //{
    //    private readonly FleetManagerDbContext _context;
    //    private readonly IRedisService _redis;
    //    private readonly ILogger<TripLocationService> _logger;
    //    private readonly IOptions<LocationTrackingSettings> _settings;
    //    private readonly IBackgroundJobClient _backgroundJobClient;
    //    private readonly ILocationFilterService _locationFilterService;

    //    // Redis key patterns
    //    private const string LOCATION_QUEUE_KEY = "location:queue";
    //    private const string LAST_UPDATE_KEY = "location:last:{0}"; // {tripId}
    //    private const string TRIP_LOCATIONS_KEY = "location:trip:{0}"; // {tripId}
    //    private const string ACTIVE_TRIPS_KEY = "trips:active";

    //    public TripLocationService(
    //        FleetManagerDbContext context,
    //        IRedisService redis,
    //        ILogger<TripLocationService> logger,
    //        IOptions<LocationTrackingSettings> settings,
    //        IBackgroundJobClient backgroundJobClient,
    //        ILocationFilterService locationFilterService)
    //    {
    //        _context = context;
    //        _redis = redis;
    //        _logger = logger;
    //        _settings = settings;
    //        _backgroundJobClient = backgroundJobClient;
    //        _locationFilterService = locationFilterService;
    //    }

    //    public async Task<MessageResponse> UpdateTripLocationAsync(LocationUpdate update)
    //    {
    //        try
    //        {
    //            var lastUpdateKey = string.Format(LAST_UPDATE_KEY, update.TripId);

    //            // ✅ 1. Check throttle using Redis (prevents DB hit)
    //            var lastUpdate = await _redis.GetAsync<DateTime?>(lastUpdateKey);
    //            var now = DateTime.UtcNow;

    //            if (lastUpdate.HasValue)
    //            {
    //                var secondsSinceLastUpdate = (now - lastUpdate.Value).TotalSeconds;
    //                if (secondsSinceLastUpdate < _settings.Value.MinUpdateIntervalSeconds)
    //                {
    //                    _logger.LogDebug(
    //                        "Location update throttled for trip {TripId}. Last update: {Seconds}s ago",
    //                        update.TripId,
    //                        Math.Round(secondsSinceLastUpdate, 1)
    //                    );

    //                    return new MessageResponse
    //                    {
    //                        Success = true,
    //                        Message = $"Update throttled. Next update in {_settings.Value.MinUpdateIntervalSeconds - (int)secondsSinceLastUpdate}s"
    //                    };
    //                }
    //            }

    //            // ✅ 2. Check if trip is active (cached in Redis)
    //            var activeTripIds = await _redis.SetMembersAsync<long>(ACTIVE_TRIPS_KEY);
    //            if (!activeTripIds.Contains(update.TripId))
    //            {
    //                // Verify in DB and cache if active
    //                var isActive = await _context.Trips
    //                    .AnyAsync(t => t.Id == update.TripId && t.Status == TripStatus.InProgress);

    //                if (!isActive)
    //                {
    //                    return new MessageResponse
    //                    {
    //                        Success = false,
    //                        Message = "Trip is not in progress"
    //                    };
    //                }

    //                // Add to active trips set
    //                await _redis.SetAddAsync(ACTIVE_TRIPS_KEY, update.TripId);
    //            }

    //            // ✅ 3. Add to Redis queue for background processing
    //            update.Timestamp = now;
    //            await _redis.ListPushAsync(LOCATION_QUEUE_KEY, update);

    //            // ✅ 4. Store latest location for immediate retrieval
    //            var latestKey = string.Format(TRIP_LOCATIONS_KEY, update.TripId) + ":latest";
    //            await _redis.SetAsync(latestKey, update, TimeSpan.FromHours(_settings.Value.LocationExpiryHours));

    //            // ✅ 5. Update last update timestamp
    //            await _redis.SetAsync(lastUpdateKey, now, TimeSpan.FromMinutes(10));

    //            // ✅ 6. Store in trip location history (limited list for real-time tracking)
    //            var historyKey = string.Format(TRIP_LOCATIONS_KEY, update.TripId);
    //            await _redis.ListPushAsync(historyKey, new TripLocationDto
    //            {
    //                TripId = update.TripId,
    //                Latitude = update.Latitude,
    //                Longitude = update.Longitude,
    //                Accuracy = update.Accuracy,
    //                Speed = update.Speed,
    //                Heading = update.Heading,
    //                Timestamp = update.Timestamp
    //            });

    //            // Keep only last 100 locations in Redis (for real-time map display)
    //            await _redis.ListTrimAsync(historyKey, -100, -1);
    //            await _redis.KeyExpireAsync(historyKey, TimeSpan.FromHours(_settings.Value.LocationExpiryHours));

    //            // ✅ 7. Check queue size and trigger background job if threshold reached
    //            var queueSize = await _redis.ListLengthAsync(LOCATION_QUEUE_KEY);
    //            if (queueSize >= _settings.Value.LocationBufferSize)
    //            {
    //                _backgroundJobClient.Enqueue<ITripLocationService>(s => s.ProcessLocationQueueAsync());
    //                _logger.LogInformation("Triggered location queue processing. Queue size: {QueueSize}", queueSize);
    //            }

    //            _logger.LogDebug(
    //                "Location queued for trip {TripId}. Queue size: {QueueSize}",
    //                update.TripId,
    //                queueSize
    //            );

    //            return new MessageResponse
    //            {
    //                Success = true,
    //                Message = "Location updated successfully"
    //            };
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(ex, "Error updating location for trip {TripId}", update.TripId);
    //            return new MessageResponse
    //            {
    //                Success = false,
    //                Message = "Error updating location"
    //            };
    //        }
    //    }
    //    public async Task ProcessLocationQueueAsync()
    //    {
    //        try
    //        {
    //            _logger.LogInformation("Starting location queue processing...");

    //            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
    //            var processedCount = 0;
    //            var savedCount = 0;
    //            var skippedCount = 0;

    //            const int batchSize = 50;
    //            var queueSize = await _redis.ListLengthAsync(LOCATION_QUEUE_KEY);

    //            if (queueSize == 0)
    //            {
    //                _logger.LogInformation("Location queue is empty");
    //                return;
    //            }

    //            _logger.LogInformation("Processing {QueueSize} location updates...", queueSize);

    //            while (queueSize > 0)
    //            {
    //                var batch = await _redis.ListRangeAsync<LocationUpdate>(LOCATION_QUEUE_KEY, 0, batchSize - 1);

    //                if (!batch.Any()) break;

    //                // Group by TripId for efficient processing
    //                var groupedByTrip = batch.GroupBy(l => l.TripId);

    //                using var transaction = await _context.Database.BeginTransactionAsync();
    //                try
    //                {
    //                    foreach (var tripGroup in groupedByTrip)
    //                    {
    //                        var tripId = tripGroup.Key;

    //                        // Get trip with vehicle
    //                        var trip = await _context.Trips
    //                            .Include(t => t.Vehicle)
    //                            .FirstOrDefaultAsync(t => t.Id == tripId);

    //                        if (trip == null || trip.Status != TripStatus.InProgress)
    //                        {
    //                            _logger.LogWarning("Trip {TripId} not found or not in progress. Skipping locations.", tripId);
    //                            skippedCount += tripGroup.Count();
    //                            continue;
    //                        }

    //                        // ✅ Get last saved checkpoint from Redis
    //                        var lastSavedKey = $"checkpoint:last:{tripId}";
    //                        var lastSaved = await _redis.GetAsync<LastSavedCheckpoint>(lastSavedKey);

    //                        // If not in Redis, get from database
    //                        if (lastSaved == null)
    //                        {
    //                            var lastCheckpoint = await _context.TripCheckpoints
    //                                .Where(c => c.TripId == tripId && c.CheckpointType == CheckpointType.Waypoint)
    //                                .OrderByDescending(c => c.CheckpointTime)
    //                                .FirstOrDefaultAsync();

    //                            if (lastCheckpoint != null && lastCheckpoint.Latitude.HasValue && lastCheckpoint.Longitude.HasValue)
    //                            {
    //                                lastSaved = new LastSavedCheckpoint
    //                                {
    //                                    Latitude = lastCheckpoint.Latitude.Value,
    //                                    Longitude = lastCheckpoint.Longitude.Value,
    //                                    Speed = null, // Not stored in old checkpoints
    //                                    Heading = null,
    //                                    Timestamp = lastCheckpoint.CheckpointTime
    //                                };
    //                            }
    //                        }

    //                        // Process each location update
    //                        foreach (var location in tripGroup.OrderBy(l => l.Timestamp))
    //                        {
    //                            try
    //                            {
    //                                processedCount++;

    //                                // ✅ Apply smart filtering
    //                                var (shouldSave, reason) = await _locationFilterService.ShouldSaveCheckpointAsync(
    //                                    tripId,
    //                                    location,
    //                                    lastSaved
    //                                );

    //                                if (!shouldSave)
    //                                {
    //                                    skippedCount++;
    //                                    _logger.LogDebug(
    //                                        "Skipped checkpoint for trip {TripId}: {Reason}",
    //                                        tripId,
    //                                        reason
    //                                    );
    //                                    continue;
    //                                }

    //                                // ✅ Save significant checkpoint
    //                                var checkpoint = new TripCheckpoint
    //                                {
    //                                    TripId = tripId,
    //                                    Location = $"GPS Update",
    //                                    Description = $"{location.Latitude:F6}, {location.Longitude:F6}",
    //                                    CheckpointTime = location.Timestamp,
    //                                    CheckpointType = CheckpointType.Waypoint,
    //                                    Latitude = location.Latitude,
    //                                    Longitude = location.Longitude,
    //                                    Notes = BuildCheckpointNotes(location, reason),
    //                                    IsActive = true,
    //                                    CreatedDate = DateTime.UtcNow,
    //                                    CreatedBy = location.UserId
    //                                };

    //                                _context.TripCheckpoints.Add(checkpoint);
    //                                savedCount++;

    //                                // ✅ Update vehicle location
    //                                if (trip.Vehicle != null)
    //                                {
    //                                    UpdateVehicleLocation(trip.Vehicle, location);
    //                                }

    //                                // ✅ Update last saved checkpoint in Redis
    //                                lastSaved = new LastSavedCheckpoint
    //                                {
    //                                    Latitude = location.Latitude,
    //                                    Longitude = location.Longitude,
    //                                    Speed = location.Speed,
    //                                    Heading = location.Heading,
    //                                    Timestamp = location.Timestamp
    //                                };

    //                                await _redis.SetAsync(
    //                                    lastSavedKey,
    //                                    lastSaved,
    //                                    TimeSpan.FromHours(_settings.Value.LocationExpiryHours)
    //                                );

    //                                _logger.LogInformation(
    //                                    "Saved checkpoint for trip {TripId}: {Reason}",
    //                                    tripId,
    //                                    reason
    //                                );
    //                            }
    //                            catch (Exception ex)
    //                            {
    //                                _logger.LogError(ex, "Error processing location for trip {TripId}", tripId);
    //                            }
    //                        }
    //                    }

    //                    await _context.SaveChangesAsync();
    //                    await transaction.CommitAsync();

    //                    // Remove processed items from queue
    //                    await _redis.ListTrimAsync(LOCATION_QUEUE_KEY, batch.Count, -1);

    //                    queueSize = await _redis.ListLengthAsync(LOCATION_QUEUE_KEY);
    //                }
    //                catch (Exception ex)
    //                {
    //                    await transaction.RollbackAsync();
    //                    _logger.LogError(ex, "Error saving location batch to database");
    //                    break;
    //                }
    //            }

    //            stopwatch.Stop();

    //            _logger.LogInformation(
    //                "Location queue processing completed. Processed: {Processed}, Saved: {Saved}, Skipped: {Skipped}, Duration: {Duration}ms, Reduction: {Reduction}%",
    //                processedCount,
    //                savedCount,
    //                skippedCount,
    //                stopwatch.ElapsedMilliseconds,
    //                processedCount > 0 ? Math.Round((skippedCount / (double)processedCount) * 100, 1) : 0
    //            );
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(ex, "Fatal error in location queue processing");
    //        }
    //    }
    //    public async Task<List<TripLocationDto>> GetTripLocationsAsync(long tripId)
    //    {
    //        try
    //        {
    //            // ✅ Try Redis first
    //            var cacheKey = string.Format(TRIP_LOCATIONS_KEY, tripId);
    //            var cachedLocations = await _redis.ListRangeAsync<TripLocationDto>(cacheKey);

    //            if (cachedLocations.Any())
    //            {
    //                _logger.LogDebug("Retrieved {Count} locations from Redis for trip {TripId}",
    //                    cachedLocations.Count, tripId);
    //                return cachedLocations;
    //            }

    //            // ✅ Fallback to database
    //            var dbLocations = await _context.TripCheckpoints
    //                .AsNoTracking()
    //                .Where(c => c.TripId == tripId && c.Latitude.HasValue && c.Longitude.HasValue)
    //                .OrderBy(c => c.CheckpointTime)
    //                .Select(c => new TripLocationDto
    //                {
    //                    TripId = c.TripId,
    //                    Latitude = c.Latitude.Value,
    //                    Longitude = c.Longitude.Value,
    //                    Timestamp = c.CheckpointTime
    //                })
    //                .ToListAsync();

    //            // ✅ Cache in Redis for future requests
    //            if (dbLocations.Any())
    //            {
    //                foreach (var loc in dbLocations.TakeLast(100)) // Last 100 locations
    //                {
    //                    await _redis.ListPushAsync(cacheKey, loc);
    //                }
    //                await _redis.KeyExpireAsync(cacheKey, TimeSpan.FromHours(_settings.Value.LocationExpiryHours));
    //            }

    //            return dbLocations;
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(ex, "Error getting locations for trip {TripId}", tripId);
    //            return new List<TripLocationDto>();
    //        }
    //    }

    //    public async Task<TripLocationDto?> GetLatestLocationAsync(long tripId)
    //    {
    //        try
    //        {
    //            var latestKey = string.Format(TRIP_LOCATIONS_KEY, tripId) + ":latest";
    //            var cached = await _redis.GetAsync<LocationUpdate>(latestKey);

    //            if (cached != null)
    //            {
    //                return new TripLocationDto
    //                {
    //                    TripId = cached.TripId,
    //                    Latitude = cached.Latitude,
    //                    Longitude = cached.Longitude,
    //                    Accuracy = cached.Accuracy,
    //                    Speed = cached.Speed,
    //                    Heading = cached.Heading,
    //                    Timestamp = cached.Timestamp
    //                };
    //            }

    //            // Fallback to DB
    //            var latest = await _context.TripCheckpoints
    //                .AsNoTracking()
    //                .Where(c => c.TripId == tripId && c.Latitude.HasValue && c.Longitude.HasValue)
    //                .OrderByDescending(c => c.CheckpointTime)
    //                .Select(c => new TripLocationDto
    //                {
    //                    TripId = c.TripId,
    //                    Latitude = c.Latitude.Value,
    //                    Longitude = c.Longitude.Value,
    //                    Timestamp = c.CheckpointTime
    //                })
    //                .FirstOrDefaultAsync();

    //            return latest;
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(ex, "Error getting latest location for trip {TripId}", tripId);
    //            return null;
    //        }
    //    }

    //    public async Task<MessageResponse> InvalidateTripCacheAsync(long tripId)
    //    {
    //        try
    //        {
    //            var keys = new[]
    //            {
    //            string.Format(LAST_UPDATE_KEY, tripId),
    //            string.Format(TRIP_LOCATIONS_KEY, tripId),
    //            string.Format(TRIP_LOCATIONS_KEY, tripId) + ":latest"
    //        };

    //            foreach (var key in keys)
    //            {
    //                await _redis.DeleteAsync(key);
    //            }

    //            // Remove from active trips if completed/cancelled
    //            var trip = await _context.Trips.FindAsync(tripId);
    //            if (trip != null && trip.Status != TripStatus.InProgress)
    //            {
    //                await _redis.SetAddAsync(ACTIVE_TRIPS_KEY, tripId);
    //            }

    //            _logger.LogInformation("Cache invalidated for trip {TripId}", tripId);

    //            return new MessageResponse { Success = true, Message = "Cache cleared" };
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(ex, "Error invalidating cache for trip {TripId}", tripId);
    //            return new MessageResponse { Success = false, Message = "Error clearing cache" };
    //        }
    //    }

    //    #region Helper Methods

    //    private string BuildCheckpointNotes(LocationUpdate location, string significanceReason)
    //    {
    //        var notes = new List<string>();

    //        // Add significance reason
    //        notes.Add($"Significance: {significanceReason}");

    //        if (location.Accuracy.HasValue)
    //        {
    //            notes.Add($"Accuracy: ±{Math.Round((double)location.Accuracy.Value, 1)}m");
    //        }

    //        if (location.Speed.HasValue)
    //        {
    //            notes.Add($"Speed: {Math.Round((double)location.Speed.Value, 1)} km/h");
    //        }

    //        if (location.Heading.HasValue)
    //        {
    //            notes.Add($"Heading: {Math.Round((double)location.Heading.Value, 0)}°");
    //        }

    //        if (!string.IsNullOrEmpty(location.DeviceId))
    //        {
    //            notes.Add($"Device: {location.DeviceId}");
    //        }

    //        return string.Join(" | ", notes);
    //    }
    //    private void UpdateVehicleLocation(Vehicle vehicle, LocationUpdate location)
    //    {
    //        var vehicleType = vehicle.GetType();
    //        var latProp = vehicleType.GetProperty("LastKnownLatitude");
    //        var lonProp = vehicleType.GetProperty("LastKnownLongitude");
    //        var updateProp = vehicleType.GetProperty("LastLocationUpdate");

    //        if (latProp != null && lonProp != null)
    //        {
    //            latProp.SetValue(vehicle, location.Latitude);
    //            lonProp.SetValue(vehicle, location.Longitude);
    //            updateProp?.SetValue(vehicle, location.Timestamp);
    //        }
    //    }

    //    #endregion
    //}
}
