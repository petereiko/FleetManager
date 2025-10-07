using FleetManager.Business.DataObjects.TripReportsDto;
using FleetManager.Business.DataObjects.TripsDto;
using FleetManager.Business.UtilityModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Interfaces.TripReportModule
{
    public interface ITripReportService
    {
        // background jobs
        Task RecomputeDailyAggregateAsync(DateTime dayUtc);
        Task RecomputeRangeAsync(DateTime startDayUtc, DateTime endDayUtc);

        // report retrieval (cached where appropriate)
        Task<List<DailyTripSummaryDto>> GetDailySummaryAsync(DateTime startUtc, DateTime endUtc, bool useCache = true);
        Task<List<DistanceByEntityDto>> GetDistanceByVehicleAsync(DateTime startUtc, DateTime endUtc, bool useCache = true);
        Task<List<DistanceByEntityDto>> GetDistanceByDriverAsync(DateTime startUtc, DateTime endUtc, bool useCache = true);
        Task<List<TripFuelDto>> GetFuelConsumptionPerTripAsync(DateTime startUtc, DateTime endUtc);
        Task<List<TripCostDto>> GetTripCostSummaryAsync(DateTime startUtc, DateTime endUtc);
        Task<List<VehicleUtilizationDto>> GetVehicleUtilizationAsync(DateTime startUtc, DateTime endUtc);
        Task<List<TopDriverDto>> GetTopDriversAsync(DateTime startUtc, DateTime endUtc, int topN = 10);

        // drilldown & paging
        Task<PaginatedResult<TripListDto>> GetTripsForVehicleAsync(long vehicleId, int page, int pageSize);
        Task<List<TripCheckpointDto>> GetCheckpointsForTripAsync(long tripId);
    }
}
