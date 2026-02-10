using DocumentFormat.OpenXml.Spreadsheet;
using FleetManager.Business.Interfaces.TripLocationModule;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Hubs
{
    /// <summary>
    /// SignalR Hub for real-time trip location tracking
    /// Supports multiple simultaneous trip tracking sessions
    /// </summary>
    //[Authorize]
    public class TripTrackingHub : Hub
    {
        private readonly ILogger<TripTrackingHub> _logger;
        private readonly ITripLocationService _locationService;

        // Track which connections are monitoring which trips
        private static readonly ConcurrentDictionary<string, HashSet<long>> _connectionTripMap = new();
        private static readonly ConcurrentDictionary<long, HashSet<string>> _tripConnectionMap = new();

        public TripTrackingHub(
            ILogger<TripTrackingHub> logger,
            ITripLocationService locationService)
        {
            _logger = logger;
            _locationService = locationService;
        }

        /// <summary>
        /// Client subscribes to track a specific trip
        /// </summary>
        public async Task TrackTrip(long tripId)
        {
            try
            {
                var connectionId = Context.ConnectionId;

                // Add connection to trip group
                await Groups.AddToGroupAsync(connectionId, $"trip-{tripId}");

                // Track the subscription
                _connectionTripMap.AddOrUpdate(
                    connectionId,
                    new HashSet<long> { tripId },
                    (_, trips) =>
                    {
                        trips.Add(tripId);
                        return trips;
                    });

                _tripConnectionMap.AddOrUpdate(
                    tripId,
                    new HashSet<string> { connectionId },
                    (_, connections) =>
                    {
                        connections.Add(connectionId);
                        return connections;
                    });

                _logger.LogInformation(
                    "Connection {ConnectionId} started tracking trip {TripId}",
                    connectionId,
                    tripId);

                // Send current location immediately
                var currentLocation = await _locationService.GetLatestLocationAsync(tripId);
                if (currentLocation != null)
                {
                    await Clients.Caller.SendAsync("LocationUpdate", currentLocation);
                }

                // Send historical route
                var historicalLocations = await _locationService.GetTripLocationsAsync(tripId);
                if (historicalLocations.Any())
                {
                    await Clients.Caller.SendAsync("RouteHistory", new
                    {
                        tripId = tripId,
                        locations = historicalLocations,
                        totalPoints = historicalLocations.Count
                    });
                }

                // Notify client of successful subscription
                await Clients.Caller.SendAsync("TrackingStarted", new
                {
                    tripId = tripId,
                    message = "Live tracking started",
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error subscribing to trip {TripId}", tripId);
                await Clients.Caller.SendAsync("TrackingError", new
                {
                    tripId = tripId,
                    error = "Failed to start tracking"
                });
            }
        }

        /// <summary>
        /// Client unsubscribes from tracking a specific trip
        /// </summary>
        public async Task UntrackTrip(long tripId)
        {
            try
            {
                var connectionId = Context.ConnectionId;

                await Groups.RemoveFromGroupAsync(connectionId, $"trip-{tripId}");

                // Remove from tracking maps
                if (_connectionTripMap.TryGetValue(connectionId, out var trips))
                {
                    trips.Remove(tripId);
                }

                if (_tripConnectionMap.TryGetValue(tripId, out var connections))
                {
                    connections.Remove(connectionId);
                }

                _logger.LogInformation(
                    "Connection {ConnectionId} stopped tracking trip {TripId}",
                    connectionId,
                    tripId);

                await Clients.Caller.SendAsync("TrackingStopped", new
                {
                    tripId = tripId,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unsubscribing from trip {TripId}", tripId);
            }
        }

        /// <summary>
        /// Get list of actively tracked trips for diagnostics
        /// </summary>
        public async Task GetActiveTracking()
        {
            var connectionId = Context.ConnectionId;

            if (_connectionTripMap.TryGetValue(connectionId, out var trips))
            {
                await Clients.Caller.SendAsync("ActiveTrips", new
                {
                    tripIds = trips.ToList(),
                    count = trips.Count
                });
            }
            else
            {
                await Clients.Caller.SendAsync("ActiveTrips", new
                {
                    tripIds = new List<long>(),
                    count = 0
                });
            }
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var connectionId = Context.ConnectionId;

            // Clean up all subscriptions for this connection
            if (_connectionTripMap.TryRemove(connectionId, out var trips))
            {
                foreach (var tripId in trips)
                {
                    if (_tripConnectionMap.TryGetValue(tripId, out var connections))
                    {
                        connections.Remove(connectionId);

                        // Clean up empty trip entries
                        if (connections.Count == 0)
                        {
                            _tripConnectionMap.TryRemove(tripId, out _);
                        }
                    }
                }
            }

            _logger.LogInformation(
                "Connection {ConnectionId} disconnected. Tracked trips cleaned up.",
                connectionId);

            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Get count of active viewers for a trip (admin only)
        /// </summary>
        [Authorize(Roles = "Admin")]
        public async Task GetTripViewers(long tripId)
        {
            var viewerCount = _tripConnectionMap.TryGetValue(tripId, out var connections)
                ? connections.Count
                : 0;

            await Clients.Caller.SendAsync("TripViewerCount", new
            {
                tripId = tripId,
                viewerCount = viewerCount
            });
        }
    }
}
