using FleetManager.Business.Database.Entities;
using FleetManager.Business.DataObjects.TripReportsDto;
using FleetManager.Business.DataObjects.TripsDto;
using FleetManager.Business.Enums;
using FleetManager.Business.Interfaces.TripModule;
using FleetManager.Business.Interfaces.TripReportModule;
using FleetManager.Business.Interfaces.UserModule;
using FleetManager.Business.UtilityModels;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Implementations.TripReportModule
{
    public class TripReportService : ITripReportService
    {
        private readonly FleetManagerDbContext _db;
        private readonly ILogger<TripReportService> _logger;
        private readonly IMemoryCache _cache;
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly ITripService _tripService;
        private readonly IAuthUser _authUser;

        // cache keys prefix
        private const string CachePrefix = "TripReport_";

        public TripReportService(
            FleetManagerDbContext db,
            ILogger<TripReportService> logger,
            IMemoryCache cache,
            IBackgroundJobClient backgroundJobClient,
            ITripService tripService,
            IAuthUser authUser)
        {
            _db = db;
            _logger = logger;
            _cache = cache;
            _backgroundJobClient = backgroundJobClient;
            _tripService = tripService;
            _authUser = authUser;
        }

        #region Background & recompute

        // Recompute aggregate for a single UTC day (midnight)
        public async Task RecomputeDailyAggregateAsync(DateTime dayUtc)
        {
            if (_authUser?.CompanyBranchId == null)
                throw new InvalidOperationException("Invalid user context.");

            var branchId = _authUser.CompanyBranchId.Value;
            var companyId = _authUser.CompanyId ?? 0;

            var dayStart = dayUtc.Date;
            var dayEnd = dayStart.AddDays(1).AddTicks(-1);

            // Query trips for that day
            var trips = await _db.Trips
                .AsNoTracking()
                .Where(t => t.CompanyBranchId == branchId && t.CreatedDate >= dayStart && t.CreatedDate <= dayEnd && t.IsActive)
                .ToListAsync();

            var aggregate = new DailyTripAggregate
            {
                DayUtc = dayStart,
                CompanyBranchId = branchId,
                CompanyId = companyId,
                TotalTrips = trips.Count,
                Scheduled = trips.Count(t => t.Status == TripStatus.Scheduled || t.Status == TripStatus.Assigned),
                Assigned = trips.Count(t => t.Status == TripStatus.Assigned),
                InProgress = trips.Count(t => t.Status == TripStatus.InProgress),
                Completed = trips.Count(t => t.Status == TripStatus.Completed),
                Cancelled = trips.Count(t => t.Status == TripStatus.Cancelled),
                TotalDistance = trips.Where(t => t.ActualDistance.HasValue).Sum(t => t.ActualDistance.Value),
                TotalFuelCost = trips.Where(t => t.ActualFuelCost.HasValue).Sum(t => t.ActualFuelCost.Value),
                CreatedDate = DateTime.UtcNow,
                ComputedDate = DateTime.UtcNow
            };

            // Upsert into DailyTripAggregates table
            var existing = await _db.DailyTripAggregates
                .FirstOrDefaultAsync(a => a.DayUtc == dayStart && a.CompanyBranchId == branchId);

            if (existing == null)
            {
                _db.DailyTripAggregates.Add(aggregate);
            }
            else
            {
                existing.TotalTrips = aggregate.TotalTrips;
                existing.Scheduled = aggregate.Scheduled;
                existing.Assigned = aggregate.Assigned;
                existing.InProgress = aggregate.InProgress;
                existing.Completed = aggregate.Completed;
                existing.Cancelled = aggregate.Cancelled;
                existing.TotalDistance = aggregate.TotalDistance;
                existing.TotalFuelCost = aggregate.TotalFuelCost;
                existing.ComputedDate = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();

            // Invalidate relevant caches
            InvalidateCacheForRange(dayStart, dayStart);
        }

        // Recompute for a range of days (inclusive)
        public async Task RecomputeRangeAsync(DateTime startDayUtc, DateTime endDayUtc)
        {
            var cur = startDayUtc.Date;
            var end = endDayUtc.Date;
            while (cur <= end)
            {
                // Enqueue background compute for each day (if you prefer immediate run, call directly)
                _backgroundJobClient.Enqueue<TripReportService>(s => s.RecomputeDailyAggregateAsync(cur));
                cur = cur.AddDays(1);
            }
        }

        #endregion

        #region Helpers - caching

        private string CacheKey(string name, DateTime s, DateTime e) => $"{CachePrefix}{name}_{s:yyyyMMdd}_{e:yyyyMMdd}_b{_authUser.CompanyBranchId}";

        private void InvalidateCacheForRange(DateTime s, DateTime e)
        {
            // simple approach: remove a few likely keys (you can be more sophisticated)
            // Remove daily summary caches
            var k1 = CacheKey("DailySummary", s, e);
            _cache.Remove(k1);
            _cache.Remove(CacheKey("DistanceByVehicle", s, e));
            _cache.Remove(CacheKey("DistanceByDriver", s, e));
            _cache.Remove(CacheKey("FuelPerTrip", s, e));
            _cache.Remove(CacheKey("TripCosts", s, e));
            _cache.Remove(CacheKey("VehicleUtilization", s, e));
            _cache.Remove(CacheKey("TopDrivers", s, e));
        }

        #endregion

        #region Report retrieval (cached)

        public async Task<List<DailyTripSummaryDto>> GetDailySummaryAsync(DateTime startUtc, DateTime endUtc, bool useCache = true)
        {
            if (_authUser?.CompanyBranchId == null) return new List<DailyTripSummaryDto>();

            var key = CacheKey("DailySummary", startUtc, endUtc);
            if (useCache && _cache.TryGetValue(key, out List<DailyTripSummaryDto> cached)) return cached;

            // Pull from precomputed aggregates when available, otherwise compute from Trips
            var branchId = _authUser.CompanyBranchId.Value;

            var aggregates = await _db.DailyTripAggregates
                .AsNoTracking()
                .Where(a => a.CompanyBranchId == branchId && a.DayUtc >= startUtc.Date && a.DayUtc <= endUtc.Date)
                .OrderBy(a => a.DayUtc)
                .ToListAsync();

            // If some days missing, compute them on the fly (lightweight)
            var results = new List<DailyTripSummaryDto>();
            var cur = startUtc.Date;
            while (cur <= endUtc.Date)
            {
                var agg = aggregates.FirstOrDefault(a => a.DayUtc == cur);
                if (agg != null)
                {
                    results.Add(new DailyTripSummaryDto
                    {
                        Date = agg.DayUtc,
                        TotalTrips = agg.TotalTrips,
                        Scheduled = agg.Scheduled,
                        Assigned = agg.Assigned,
                        InProgress = agg.InProgress,
                        Completed = agg.Completed,
                        Cancelled = agg.Cancelled,
                        TotalDistance = agg.TotalDistance,
                        TotalFuelCost = agg.TotalFuelCost
                    });
                }
                else
                {
                    // fallback compute
                    var dayStart = cur;
                    var dayEnd = cur.AddDays(1).AddTicks(-1);
                    var trips = await _db.Trips
                        .AsNoTracking()
                        .Where(t => t.CompanyBranchId == branchId && t.CreatedDate >= dayStart && t.CreatedDate <= dayEnd && t.IsActive)
                        .ToListAsync();
                    results.Add(new DailyTripSummaryDto
                    {
                        Date = cur,
                        TotalTrips = trips.Count,
                        Scheduled = trips.Count(t => t.Status == TripStatus.Scheduled || t.Status == TripStatus.Assigned),
                        Assigned = trips.Count(t => t.Status == TripStatus.Assigned),
                        InProgress = trips.Count(t => t.Status == TripStatus.InProgress),
                        Completed = trips.Count(t => t.Status == TripStatus.Completed),
                        Cancelled = trips.Count(t => t.Status == TripStatus.Cancelled),
                        TotalDistance = trips.Where(t => t.ActualDistance.HasValue).Sum(t => t.ActualDistance.Value),
                        TotalFuelCost = trips.Where(t => t.ActualFuelCost.HasValue).Sum(t => t.ActualFuelCost.Value)
                    });
                }

                cur = cur.AddDays(1);
            }

            // cache short term
            _cache.Set(key, results, TimeSpan.FromMinutes(10));
            return results;
        }

        public async Task<List<DistanceByEntityDto>> GetDistanceByVehicleAsync(DateTime startUtc, DateTime endUtc, bool useCache = true)
        {
            if (_authUser?.CompanyBranchId == null) return new List<DistanceByEntityDto>();

            var key = CacheKey("DistanceByVehicle", startUtc, endUtc);
            if (useCache && _cache.TryGetValue(key, out List<DistanceByEntityDto> cached)) return cached;

            var branchId = _authUser.CompanyBranchId.Value;
            var q = _db.Trips
                .AsNoTracking()
                .Include(t => t.Vehicle).ThenInclude(v => v.VehicleMake)
                .Include(t => t.Vehicle).ThenInclude(v => v.VehicleModel)
                .Where(t => t.CompanyBranchId == branchId && t.IsActive && t.ActualDistance.HasValue
                            && t.CreatedDate >= startUtc && t.CreatedDate <= endUtc);

            var result = await q
                //.GroupBy(t => new { t.VehicleId, Plate = t.Vehicle != null ? (t.Vehicle.VehicleMake.Name + " " +t.Vehicle.VehicleModel.Name + " " + t.Vehicle.PlateNo) : "" })
                .GroupBy(t => new { t.VehicleId, Plate = t.Vehicle != null ? (t.Vehicle.CustomMakeName != null ? (t.Vehicle.CustomMakeName + " " + t.Vehicle.CustomModelName).Trim() + " " + t.Vehicle.PlateNo
                    : (t.Vehicle.VehicleMake != null ? t.Vehicle.VehicleMake.Name : "Unknown") + " " + (t.Vehicle.VehicleModel != null ? t.Vehicle.VehicleModel.Name : "") + " " + t.Vehicle.PlateNo)  : "" })
                .Select(g => new DistanceByEntityDto
                {
                    EntityId = g.Key.VehicleId,
                    EntityName = g.Key.Plate,
                    TotalDistance = g.Sum(t => t.ActualDistance.Value),
                    TripCount = g.Count()
                })
                .OrderByDescending(x => x.TotalDistance)
                .ToListAsync();

            _cache.Set(key, result, TimeSpan.FromMinutes(10));
            return result;
        }

        public async Task<List<DistanceByEntityDto>> GetDistanceByDriverAsync(DateTime startUtc, DateTime endUtc, bool useCache = true)
        {
            if (_authUser?.CompanyBranchId == null) return new List<DistanceByEntityDto>();

            var key = CacheKey("DistanceByDriver", startUtc, endUtc);
            if (useCache && _cache.TryGetValue(key, out List<DistanceByEntityDto> cached)) return cached;

            var branchId = _authUser.CompanyBranchId.Value;
            var q = _db.Trips
                .AsNoTracking()
                .Include(t => t.Driver).ThenInclude(d => d.User)
                .Where(t => t.CompanyBranchId == branchId && t.IsActive && t.ActualDistance.HasValue
                            && t.CreatedDate >= startUtc && t.CreatedDate <= endUtc);

            var result = await q
                .GroupBy(t => new { t.DriverId, Name = t.Driver != null ? (t.Driver.User.FirstName + " " + t.Driver.User.LastName) : "Unassigned" })
                .Select(g => new DistanceByEntityDto
                {
                    EntityId = g.Key.DriverId ?? 0,
                    EntityName = g.Key.Name,
                    TotalDistance = g.Sum(t => t.ActualDistance.Value),
                    TripCount = g.Count()
                })
                .OrderByDescending(x => x.TotalDistance)
                .ToListAsync();

            _cache.Set(key, result, TimeSpan.FromMinutes(10));
            return result;
        }

        public async Task<List<TripFuelDto>> GetFuelConsumptionPerTripAsync(DateTime startUtc, DateTime endUtc)
        {
            if (_authUser?.CompanyBranchId == null) return new List<TripFuelDto>();

            var branchId = _authUser.CompanyBranchId.Value;
            var q = _db.Trips
                .AsNoTracking()
                .Include(t => t.Vehicle).ThenInclude(v => v.VehicleMake)
                .Include(t => t.Vehicle).ThenInclude(v => v.VehicleModel)
                .Include(t => t.Driver).ThenInclude(d => d.User)
                .Where(t => t.CompanyBranchId == branchId && t.IsActive && t.CreatedDate >= startUtc && t.CreatedDate <= endUtc);

            var list = await q.Select(t => new TripFuelDto
            {
                TripId = t.Id,
                TripNumber = t.TripNumber,
                DriverId = t.DriverId,
                DriverName = t.Driver != null ? t.Driver.User.FirstName + " " + t.Driver.User.LastName : null,
                VehicleId = t.VehicleId,
                //VehiclePlateNo = t.Vehicle != null ? t.Vehicle.VehicleMake.Name + " " + t.Vehicle.VehicleModel.Name + " " + t.Vehicle.PlateNo : null,
                VehiclePlateNo = t.Vehicle != null ? t.Vehicle.CustomMakeName != null ? (t.Vehicle.CustomMakeName + " " + t.Vehicle.CustomModelName).Trim() + " " + t.Vehicle.PlateNo
                : (t.Vehicle.VehicleMake != null ? t.Vehicle.VehicleMake.Name : "Unknown") + " " + (t.Vehicle.VehicleModel != null ? t.Vehicle.VehicleModel.Name : "") + " " + t.Vehicle.PlateNo  : null,
                ActualDistance = t.ActualDistance,
                ActualFuelCost = t.ActualFuelCost,
                CreatedDate = t.CreatedDate
            }).OrderByDescending(t => t.CreatedDate).ToListAsync();

            return list;
        }

        public async Task<List<TripCostDto>> GetTripCostSummaryAsync(DateTime startUtc, DateTime endUtc)
        {
            if (_authUser?.CompanyBranchId == null) return new List<TripCostDto>();

            var branchId = _authUser.CompanyBranchId.Value;
            var q = _db.Trips
                .AsNoTracking()
                .Where(t => t.CompanyBranchId == branchId && t.IsActive && t.CreatedDate >= startUtc && t.CreatedDate <= endUtc);

            var list = await q.Select(t => new TripCostDto
            {
                TripId = t.Id,
                TripNumber = t.TripNumber,
                EstimatedFuelCost = t.EstimatedFuelCost,
                ActualFuelCost = t.ActualFuelCost
            }).ToListAsync();

            return list;
        }

        public async Task<List<VehicleUtilizationDto>> GetVehicleUtilizationAsync(DateTime startUtc, DateTime endUtc)
        {
            if (_authUser?.CompanyBranchId == null) return new List<VehicleUtilizationDto>();

            var branchId = _authUser.CompanyBranchId.Value;

            // Use trips that have ActualStartDate/ActualEndDate to compute usage duration
            var q = _db.Trips
                .AsNoTracking()
                .Include(t => t.Vehicle).ThenInclude(v => v.VehicleMake)
                .Include(t => t.Vehicle).ThenInclude(v => v.VehicleModel)
                .Where(t => t.CompanyBranchId == branchId && t.IsActive
                            && t.ActualStartDate.HasValue && t.ActualEndDate.HasValue
                            && t.ActualStartDate >= startUtc && t.ActualEndDate <= endUtc);

            var list = await q
                //.GroupBy(t => new { t.VehicleId, Plate = t.Vehicle != null ? (t.Vehicle.VehicleMake.Name + " " + t.Vehicle.VehicleModel.Name + " " + t.Vehicle.PlateNo) : "" })
                .GroupBy(t => new {
                    t.VehicleId,
                    Plate = t.Vehicle != null ? (t.Vehicle.CustomMakeName != null ? (t.Vehicle.CustomMakeName + " " + t.Vehicle.CustomModelName).Trim() + " " + t.Vehicle.PlateNo
                    : (t.Vehicle.VehicleMake != null ? t.Vehicle.VehicleMake.Name : "Unknown") + " " + (t.Vehicle.VehicleModel != null ? t.Vehicle.VehicleModel.Name : "") + " " + t.Vehicle.PlateNo) : ""
                })
                .Select(g => new VehicleUtilizationDto
                {
                    VehicleId = g.Key.VehicleId,
                    VehiclePlateNo = g.Key.Plate,
                    TotalUsageHours = TimeSpan.FromSeconds(g.Sum(t => EF.Functions.DateDiffSecond(t.ActualStartDate.Value, t.ActualEndDate.Value))),
                    TotalDistance = g.Sum(t => t.ActualDistance ?? 0),
                    TripCount = g.Count()
                })
                .OrderByDescending(v => v.TotalUsageHours)
                .ToListAsync();

            return list;
        }

        public async Task<List<TopDriverDto>> GetTopDriversAsync(DateTime startUtc, DateTime endUtc, int topN = 10)
        {
            if (_authUser?.CompanyBranchId == null) return new List<TopDriverDto>();

            var branchId = _authUser.CompanyBranchId.Value;

            var q = _db.Trips
                .AsNoTracking()
                .Include(t => t.Driver).ThenInclude(d => d.User)
                .Where(t => t.CompanyBranchId == branchId && t.IsActive && t.CreatedDate >= startUtc && t.CreatedDate <= endUtc && t.DriverId != null);

            var list = await q
                .GroupBy(t => new { t.DriverId, Name = t.Driver != null ? (t.Driver.User.FirstName + " " + t.Driver.User.LastName) : "" })
                .Select(g => new TopDriverDto
                {
                    DriverId = g.Key.DriverId ?? 0,
                    DriverName = g.Key.Name,
                    TripCount = g.Count(),
                    TotalDistance = g.Where(t => t.ActualDistance.HasValue).Sum(t => t.ActualDistance.Value),
                    TotalFuelCost = g.Where(t => t.ActualFuelCost.HasValue).Sum(t => t.ActualFuelCost.Value)
                })
                .OrderByDescending(d => d.TripCount)
                .Take(topN)
                .ToListAsync();

            return list;
        }

        #endregion

        #region Drilldown & paging

        public async Task<PaginatedResult<TripListDto>> GetTripsForVehicleAsync(long vehicleId, int page, int pageSize)
        {
            if (_authUser?.CompanyBranchId == null) return new PaginatedResult<TripListDto>();

            var branchId = _authUser.CompanyBranchId.Value;

            var query = _db.Trips
                .AsNoTracking()
                .Include(t => t.Vehicle).ThenInclude(v => v.VehicleMake)
                .Include(t => t.Vehicle).ThenInclude(v => v.VehicleModel)
                .Include(t => t.Driver).ThenInclude(d => d.User)
                .Where(t => t.VehicleId == vehicleId && t.CompanyBranchId == branchId && t.IsActive)
                .OrderByDescending(t => t.ScheduledStartDate);

            // reuse PaginatedResult.CreateAsync (you have this helper)
            return await PaginatedResult<TripListDto>.CreateAsync(query.Select(t => new TripListDto
            {
                Id = t.Id,
                TripNumber = t.TripNumber,
                VehiclePlateNo = t.Vehicle != null ? t.Vehicle.CustomMakeName != null ? (t.Vehicle.CustomMakeName + " " + t.Vehicle.CustomModelName).Trim() + " " + t.Vehicle.PlateNo
                : (t.Vehicle.VehicleMake != null ? t.Vehicle.VehicleMake.Name : "Unknown") + " " + (t.Vehicle.VehicleModel != null ? t.Vehicle.VehicleModel.Name : "") + " " + t.Vehicle.PlateNo : "",
                //VehiclePlateNo = t.Vehicle != null ? t.Vehicle.VehicleMake.Name + " " + t.Vehicle.VehicleModel.Name + " " + t.Vehicle.PlateNo : "",
                DriverName = t.Driver != null ? t.Driver.User.FirstName + " " + t.Driver.User.LastName : null,
                Origin = t.Origin,
                Destination = t.Destination,
                ScheduledStartDate = t.ScheduledStartDate,
                ScheduledEndDate = t.ScheduledEndDate,
                Status = t.Status,
                StatusDisplay = t.Status.ToString(),
                Priority = t.Priority,
                PriorityDisplay = t.Priority.ToString(),
                CreatedDate = t.CreatedDate
            }), page, pageSize);
        }

        public async Task<List<TripCheckpointDto>> GetCheckpointsForTripAsync(long tripId)
        {
            if (_authUser?.CompanyBranchId == null) return new List<TripCheckpointDto>();

            // ensure trip belongs to branch
            var trip = await _db.Trips.AsNoTracking().FirstOrDefaultAsync(t => t.Id == tripId && t.CompanyBranchId == _authUser.CompanyBranchId && t.IsActive);
            if (trip == null) return new List<TripCheckpointDto>();

            var cps = await _db.TripCheckpoints
                .AsNoTracking()
                .Where(c => c.TripId == tripId && c.IsActive && c.Latitude != null && c.Longitude != null)
                .OrderBy(c => c.CheckpointTime)
                .Select(c => new TripCheckpointDto
                {
                    Id = c.Id,
                    TripId = c.TripId,
                    Location = c.Location,
                    Description = c.Description,
                    CheckpointTime = c.CheckpointTime,
                    CheckpointType = c.CheckpointType,
                    CheckpointTypeDisplay = c.CheckpointType.ToString(),
                    Notes = c.Notes
                })
                .ToListAsync();

            return cps;
        }

        #endregion
    }
}
