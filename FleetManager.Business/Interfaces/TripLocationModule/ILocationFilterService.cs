using FleetManager.Business.DataObjects.TripsDto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Interfaces.TripLocationModule
{
    public interface ILocationFilterService
    {
        Task<(bool ShouldSave, string Reason)> ShouldSaveCheckpointAsync(
            long tripId,
            LocationUpdate current,
            LastSavedCheckpoint? lastSaved
        );
    }
}
