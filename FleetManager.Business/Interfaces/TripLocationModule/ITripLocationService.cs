using FleetManager.Business.DataObjects.TripsDto;
using FleetManager.Business.UtilityModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Interfaces.TripLocationModule
{
    public interface ITripLocationService
    {
        Task<MessageResponse> UpdateTripLocationAsync(LocationUpdate update);
        Task<List<TripLocationDto>> GetTripLocationsAsync(long tripId);
        Task<TripLocationDto?> GetLatestLocationAsync(long tripId);
        Task ProcessLocationQueueAsync(); // Called by Hangfire
        Task<MessageResponse> InvalidateTripCacheAsync(long tripId);
    }
}
