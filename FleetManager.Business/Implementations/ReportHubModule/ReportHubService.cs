using FleetManager.Business.DataObjects.ReportsCenter;
using FleetManager.Business.DataObjects.TripsDto;
using FleetManager.Business.Enums;
using FleetManager.Business.Interfaces.ReportHubModule;
using FleetManager.Business.Interfaces.UserModule;
using FleetManager.Business.UtilityModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Implementations.ReportHubModule
{
    

    public class ReportHubService : IReportHubService
    {
        private readonly FleetManagerDbContext _db;
        private readonly IAuthUser _authUser;
        private readonly ILogger<ReportHubService> _logger;
        private readonly IMemoryCache _cache;

        // cache durations - tweak as needed
        private static readonly TimeSpan SearchCacheDuration = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan SingleItemCacheDuration = TimeSpan.FromHours(1);


        public ReportHubService(FleetManagerDbContext db, IAuthUser authUser, ILogger<ReportHubService> logger, IMemoryCache cache)
        {
            _db = db;
            _authUser = authUser;
            _logger = logger;
            _cache = cache;
        }

        #region CacheHelper
        // --- Helpers for cache keys / version tokens ---
        private static string DriverVersionKey(long branchId) => $"drivers_version_{branchId}";
        private static string VehicleVersionKey(long branchId) => $"vehicles_version_{branchId}";

        private string GetDriverCacheKey(long branchId, string q, int page, int pageSize)
            => $"search_drivers_{branchId}_{GetBranchVersion(DriverVersionKey(branchId))}_{(q ?? "").Trim().ToLowerInvariant()}_{page}_{pageSize}";

        private string GetVehicleCacheKey(long branchId, string q, int page, int pageSize)
            => $"search_vehicles_{branchId}_{GetBranchVersion(VehicleVersionKey(branchId))}_{(q ?? "").Trim().ToLowerInvariant()}_{page}_{pageSize}";

        private string GetDriverByIdCacheKey(long branchId, long id)
            => $"driver_byid_{branchId}_{GetBranchVersion(DriverVersionKey(branchId))}_{id}";

        private string GetVehicleByIdCacheKey(long branchId, long id)
            => $"vehicle_byid_{branchId}_{GetBranchVersion(VehicleVersionKey(branchId))}_{id}";

        // retrieves the version token for a branch; if missing returns "v0"
        private string GetBranchVersion(string versionKey)
        {
            if (_cache.TryGetValue<string>(versionKey, out var v) && !string.IsNullOrWhiteSpace(v))
                return v!;
            return "v0";
        }

        // bump the branch version token (invalidate all previous keys for that branch)
        private void BumpBranchVersion(string versionKey)
        {
            _cache.Set(versionKey, Guid.NewGuid().ToString(), new MemoryCacheEntryOptions
            {
                // we keep version key around long enough; 24 hours is usually fine
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
            });
        }

        #endregion

        public async Task<DailyFleetActivityReportDto> GetDailyFleetActivityAsync(DateTime date, ReportFilter filter = null)
        {
            var branchId = _authUser.CompanyBranchId;
            var dayStart = date.Date;
            var dayEnd = dayStart.AddDays(1).AddTicks(-1);

            var totalVehicles = await _db.Vehicles.AsNoTracking().CountAsync(v => v.CompanyBranchId == branchId);

            var tripsQ = _db.Trips.AsNoTracking()
                .Where(t => t.CompanyBranchId == branchId && t.ActualEndDate != null && t.ActualEndDate >= dayStart && t.ActualEndDate <= dayEnd);

            if (filter?.DriverId != null) tripsQ = tripsQ.Where(t => t.DriverId == filter.DriverId);
            if (filter?.VehicleId != null) tripsQ = tripsQ.Where(t => t.VehicleId == filter.VehicleId);

            var tripsCompleted = await tripsQ.CountAsync();
            var totalDistance = await tripsQ.SumAsync(t => (decimal?)t.ActualDistance) ?? 0;

            var vehicleActivities = await tripsQ
                .GroupBy(t => t.VehicleId)
                .Select(g => new VehicleActivityDto
                {
                    VehicleId = g.Key,
                    PlateNo = g.Select(x => x.Vehicle.PlateNo).FirstOrDefault(),
                    VehicleMake = g.Select(x => x.Vehicle.VehicleMake.Name).FirstOrDefault(),
                    VehicleModel = g.Select(x => x.Vehicle.VehicleModel.Name).FirstOrDefault(),
                    DistanceKm = g.Sum(x => (decimal?)x.ActualDistance) ?? 0,
                    TripsCount = g.Count(),
                    Status = g.Any(x => x.Status == TripStatus.InProgress) ? "Active" : "Completed"
                }).ToListAsync();

            // simple active vehicles calculation: vehicles with trips in last 4 hours
            var activeVehicleIds = await _db.Trips.AsNoTracking()
                .Where(t => t.CompanyBranchId == branchId && t.ActualEndDate >= dayStart.AddHours(-4) && t.ActualEndDate <= dayEnd)
                .Select(t => t.VehicleId).Distinct().ToListAsync();

            return new DailyFleetActivityReportDto
            {
                Date = date.Date,
                TotalVehicles = totalVehicles,
                ActiveVehicles = activeVehicleIds.Count,
                IdleVehicles = Math.Max(0, totalVehicles - activeVehicleIds.Count),
                TripsCompleted = tripsCompleted,
                TotalDistanceKm = totalDistance,
                VehicleActivities = vehicleActivities
            };
        }

        public async Task<PaginatedResult<DriverPerformanceReportDto>> GetDriverPerformanceAsync(
    DateTime from, DateTime to, ReportFilter filter, int page = 1, int pageSize = 25)
        {
            if (!_authUser.CompanyBranchId.HasValue)
                throw new InvalidOperationException("Missing CompanyBranchId for current user.");

            var branchId = _authUser.CompanyBranchId.Value;

            var baseTrips = _db.Trips.AsNoTracking()
                .Where(t => t.CompanyBranchId == branchId
                            && t.ActualStartDate != null
                            && t.ActualEndDate != null
                            && t.ActualEndDate >= from
                            && t.ActualEndDate <= to);

            if (filter?.DriverId != null) baseTrips = baseTrips.Where(t => t.DriverId == filter.DriverId);
            if (filter?.VehicleId != null) baseTrips = baseTrips.Where(t => t.VehicleId == filter.VehicleId);

            var grouped = baseTrips
                .GroupBy(t => t.DriverId)
                .Select(g => new DriverPerformanceReportDto
                {
                    DriverId = g.Key.Value,
                    DriverName = g.Select(x => (x.Driver.User.FirstName ?? "") + " " + (x.Driver.User.LastName ?? "")).FirstOrDefault(),
                    TripsCount = g.Count(),
                    TotalDistanceKm = g.Sum(x => (decimal?)x.ActualDistance) ?? 0m,

                    // CORRECT: sum minutes in SQL, coalesce to 0, then divide by 60
                    TotalHours = ((double?)(g.Sum(x => (int?)EF.Functions.DateDiffMinute(x.ActualStartDate, x.ActualEndDate))) ?? 0.0) / 60.0,

                    IncidentsCount = _db.DriverViolations
                                        .AsNoTracking()
                                        .Count(v => v.DriverId == g.Key && v.CreatedDate >= from && v.CreatedDate <= to)
                })
                .OrderByDescending(x => x.TripsCount);

            return await PaginatedResult<DriverPerformanceReportDto>.CreateAsync(grouped, page, pageSize);
        }

        public async Task<FuelConsumptionReportDto> GetFuelConsumptionAsync(DateTime from, DateTime to, ReportFilter filter)
        {
            if (!_authUser.CompanyBranchId.HasValue)
                throw new InvalidOperationException("Missing CompanyBranchId in auth user.");

            var branchId = _authUser.CompanyBranchId.Value;

            // Fuel logs for this branch & date range
            var fuelQ = _db.FuelLogs.AsNoTracking()
                .Where(f => f.Vehicle.CompanyBranchId == branchId && f.Date >= from && f.Date <= to);

            if (filter?.VehicleId != null) fuelQ = fuelQ.Where(f => f.VehicleId == filter.VehicleId);
            if (filter?.DriverId != null) fuelQ = fuelQ.Where(f => f.DriverId == filter.DriverId);

            var totalVolume = await fuelQ.SumAsync(f => (decimal?)f.Volume) ?? 0m;
            var totalCost = await fuelQ.SumAsync(f => (decimal?)f.Cost) ?? 0m;
            var avgPrice = totalVolume == 0 ? 0m : totalCost / totalVolume;

            // Distances per vehicle from Trips (branch scoped)
            var distances = await _db.Trips.AsNoTracking()
                .Where(t => t.CompanyBranchId == branchId && t.ActualEndDate != null && t.ActualEndDate >= from && t.ActualEndDate <= to)
                .GroupBy(t => t.VehicleId)
                .Select(g => new { VehicleId = g.Key, Distance = g.Sum(x => (decimal?)x.ActualDistance) ?? 0m })
                .ToListAsync();

            // Group fuel logs by vehicle and include vehicle plate, make and model via the navigation properties
            var byVehicle = await fuelQ
                .GroupBy(f => f.VehicleId)
                .Select(g => new
                {
                    VehicleId = g.Key.Value,
                    PlateNo = g.Select(x => x.Vehicle.PlateNo).FirstOrDefault(),
                    Make = g.Select(x => x.Vehicle.VehicleMake.Name).FirstOrDefault(),
                    Model = g.Select(x => x.Vehicle.VehicleModel.Name).FirstOrDefault(),
                    Volume = g.Sum(x => (decimal?)x.Volume) ?? 0m,
                    Cost = g.Sum(x => (decimal?)x.Cost) ?? 0m
                })
                .ToListAsync();

            // Build the final DTO list with combined make/model/plate and distance lookup
            var fuelByVehicle = byVehicle.Select(bv =>
            {
                var d = distances.FirstOrDefault(x => x.VehicleId == bv.VehicleId)?.Distance ?? 0m;
                var plateLabel = string.IsNullOrWhiteSpace(bv.Make) && string.IsNullOrWhiteSpace(bv.Model)
                    ? bv.PlateNo ?? ""
                    : $"{bv.Make} {bv.Model} {(!string.IsNullOrWhiteSpace(bv.PlateNo) ? $"({bv.PlateNo})" : "")}";

                return new FuelByVehicleDto
                {
                    VehicleId = bv.VehicleId,
                    PlateNo = plateLabel,
                    Volume = bv.Volume,
                    Cost = bv.Cost,
                    DistanceKm = d
                };
            }).ToList();

            var totalDistance = fuelByVehicle.Sum(x => x.DistanceKm);
            var costPerKm = totalDistance == 0 ? 0m : fuelByVehicle.Sum(x => x.Cost) / totalDistance;

            return new FuelConsumptionReportDto
            {
                TotalVolume = totalVolume,
                TotalCost = totalCost,
                AveragePricePerUnit = avgPrice,
                ByVehicle = fuelByVehicle,
                CostPerKm = costPerKm
            };
        }

        public async Task<PaginatedResult<TripSummaryDto>> GetTripSummaryAsync(DateTime from, DateTime to, ReportFilter filter, int page = 1, int pageSize = 25)
        {
            var branchId = _authUser.CompanyBranchId;
            var q = _db.Trips.AsNoTracking().Where(t => t.CompanyBranchId == branchId && t.CreatedDate >= from && t.CreatedDate <= to);

            if (filter?.DriverId != null) q = q.Where(t => t.DriverId == filter.DriverId);
            if (filter?.VehicleId != null) q = q.Where(t => t.VehicleId == filter.VehicleId);
            if (!string.IsNullOrWhiteSpace(filter?.Route)) q = q.Where(t => t.Origin.Contains(filter.Route) || t.Destination.Contains(filter.Route));

            var projection = q.OrderByDescending(t => t.CreatedDate)
                .Select(t => new TripSummaryDto
                {
                    TripId = t.Id,
                    TripNumber = t.TripNumber,
                    DriverName = t.Driver.User.FirstName + " " + t.Driver.User.LastName,
                    VehiclePlate = t.Vehicle.PlateNo,
                    Origin = t.Origin,
                    Destination = t.Destination,
                    ScheduledStart = t.ScheduledStartDate,
                    ActualStart = t.ActualStartDate,
                    ActualEnd = t.ActualEndDate,
                    ActualDistance = t.ActualDistance,
                    EstimatedDistance = t.EstimatedDistance,
                    EstimatedFuelCost = t.EstimatedFuelCost,
                    ActualFuelCost = t.ActualFuelCost
                });

            return await PaginatedResult<TripSummaryDto>.CreateAsync(projection, page, pageSize);
        }

        public async Task<CostAnalysisDto> GetCostAnalysisAsync(DateTime from, DateTime to, ReportFilter filter)
        {
            var branchId = _authUser.CompanyBranchId;

            // Fuel
            var fuelQ = _db.FuelLogs.AsNoTracking().Where(f => f.Vehicle.CompanyBranchId == branchId && f.Date >= from && f.Date <= to);
            if (filter?.VehicleId != null) fuelQ = fuelQ.Where(f => f.VehicleId == filter.VehicleId);
            var fuelCost = await fuelQ.SumAsync(f => (decimal?)f.Cost) ?? 0;

            // Maintenance: use MaintenanceRecords or Invoices (you have MaintenanceRecord & Invoice models)
            var maintenanceCost = await _db.MaintenanceRecords.AsNoTracking()
                .Where(m => m.Vehicle.CompanyBranchId == branchId && m.CreatedDate >= from && m.CreatedDate <= to)
                .SumAsync(m => (decimal?)m.Cost) ?? 0;

            // Fines & Toll
            var fines = await _db.FineAndTolls.AsNoTracking()
                .Where(f => f.CompanyBranchId == branchId && f.CreatedDate >= from && f.CreatedDate <= to)
                .SumAsync(f => (decimal?)f.Amount) ?? 0;

            // Other costs (trip expenses)
            var tripExpenses = await _db.TripExpenses.AsNoTracking()
                .Where(e => e.Trip.CompanyBranchId == branchId && e.CreatedDate >= from && e.CreatedDate <= to)
                .SumAsync(e => (decimal?)e.Amount) ?? 0;

            var byVehicle = await _db.Vehicles.AsNoTracking()
                .Where(v => v.CompanyBranchId == branchId)
                .Select(v => new CostByVehicleDto
                {
                    VehicleId = v.Id,
                    PlateNo = v.PlateNo,
                    FuelCost = fuelQ.Where(f => f.VehicleId == v.Id).Sum(f => (decimal?)f.Cost) ?? 0,
                    MaintenanceCost = _db.MaintenanceRecords.Where(m => m.VehicleId == v.Id && m.CreatedDate >= from && m.CreatedDate <= to).Sum(m => (decimal?)m.Cost) ?? 0,
                    TollsAndFines = _db.FineAndTolls.Where(ft => ft.VehicleId == v.Id && ft.CreatedDate >= from && ft.CreatedDate <= to).Sum(ft => (decimal?)ft.Amount) ?? 0
                }).ToListAsync();

            return new CostAnalysisDto
            {
                FuelCost = fuelCost,
                MaintenanceCost = maintenanceCost,
                TollsAndFines = fines,
                OtherCosts = tripExpenses,
                ByVehicle = byVehicle
            };
        }

        //public async Task<PaginatedResult<IncidentReportDto>> GetIncidentReportAsync(DateTime from, DateTime to, ReportFilter filter, int page = 1, int pageSize = 25)
        //{
        //    var branchId = _authUser.CompanyBranchId;
        //    var q = _db.Incidents.AsNoTracking().Where(i => i.CompanyBranchId == branchId && i.Date >= from && i.Date <= to);

        //    if (filter?.VehicleId != null) q = q.Where(i => i.VehicleId == filter.VehicleId);
        //    if (filter?.DriverId != null) q = q.Where(i => i.DriverId == filter.DriverId);

        //    var projection = q.OrderByDescending(i => i.Date)
        //        .Select(i => new IncidentReportDto
        //        {
        //            IncidentId = i.Id,
        //            Date = i.Date,
        //            Type = i.Type.ToString(),
        //            Location = i.Location,
        //            Description = i.Description,
        //            Impact = i.Impact,
        //            VehicleId = i.VehicleId,
        //            VehiclePlate = i.Vehicle.PlateNo,
        //            DriverId = i.DriverId,
        //            DriverName = i.Driver.User.FirstName + " " + i.Driver.User.LastName
        //        });

        //    return await PaginatedResult<IncidentReportDto>.CreateAsync(projection, page, pageSize);
        //}

        public async Task<PaginatedResult<VehicleInspectionDto>> GetVehicleInspectionReportAsync(DateTime from, DateTime to, ReportFilter filter, int page = 1, int pageSize = 25)
        {
            var branchId = _authUser.CompanyBranchId;
            var q = _db.DriverDutyOfCares.AsNoTracking().Where(d => d.Vehicle.CompanyBranchId == branchId && d.Date >= from && d.Date <= to);

            if (filter?.VehicleId != null) q = q.Where(d => d.VehicleId == filter.VehicleId);
            if (filter?.DriverId != null) q = q.Where(d => d.DriverId == filter.DriverId);

            var projection = q.OrderByDescending(d => d.Date)
                .Select(d => new VehicleInspectionDto
                {
                    InspectionId = d.Id,
                    Date = d.Date,
                    VehicleId = d.VehicleId ?? 0,
                    PlateNo = d.Vehicle.PlateNo,
                    Inspector = d.CreatedBy,
                    Passed = d.DutyOfCareStatus == DriverDutyOfCareStatus.Compliant,
                    Notes = d.VehicleConditionNotes
                });

            return await PaginatedResult<VehicleInspectionDto>.CreateAsync(projection, page, pageSize);
        }

        public async Task<List<DriverLicenseExpiryDto>> GetDriverLicenseExpiryAsync(DateTime from, DateTime to)
        {
            var branchId = _authUser.CompanyBranchId;
            var q = _db.Drivers.AsNoTracking()
                .Where(d => d.CompanyBranchId == branchId && d.LicenseExpiryDate != null && d.LicenseExpiryDate >= from && d.LicenseExpiryDate <= to)
                .Select(d => new DriverLicenseExpiryDto
                {
                    DriverId = d.Id,
                    DriverName = d.User.FirstName + " " + d.User.LastName,
                    LicenseNumber = d.LicenseNumber,
                    LicenseExpiryDate = d.LicenseExpiryDate,
                    Status = d.LicenseExpiryDate < DateTime.UtcNow ? "Expired" : "ExpiringSoon"
                });

            return await q.ToListAsync();
        }

        public async Task<List<VehicleDocumentationDto>> GetVehicleDocumentationReportAsync()
        {
            var branchId = _authUser.CompanyBranchId;
            var q = _db.Vehicles.AsNoTracking()
                .Where(v => v.CompanyBranchId == branchId)
                .Select(v => new VehicleDocumentationDto
                {
                    VehicleId = v.Id,
                    PlateNo = v.PlateNo,
                    InsuranceExpiryDate = v.InsuranceExpiryDate,
                    RoadWorthyExpiryDate = v.RoadWorthyExpiryDate
                });

            return await q.ToListAsync();
        }

        public async Task<List<VehicleUtilizationDto>> GetVehicleUtilizationAsync(DateTime from, DateTime to, ReportFilter filter)
        {
            var branchId = _authUser.CompanyBranchId;

            // Basic util: sum trip durations per vehicle / total available hours in period
            var totalHours = (to - from).TotalHours;
            if (totalHours <= 0) totalHours = 1;

            var trips = await _db.Trips.AsNoTracking()
                .Include(t => t.Vehicle).ThenInclude(v => v.VehicleMake)
                .Include(t => t.Vehicle).ThenInclude(v => v.VehicleModel)
                .Where(t => t.CompanyBranchId == branchId && t.ActualStartDate != null && t.ActualEndDate != null && t.ActualStartDate >= from && t.ActualEndDate <= to)
                .ToListAsync();

            var groups = trips.GroupBy(t => t.VehicleId)
                .Select(g =>
                {
                    var durationHours = g.Sum(x => (x.ActualEndDate.Value - x.ActualStartDate.Value).TotalHours);
                    var vehicle = g.Select(x => x.Vehicle).FirstOrDefault();
                    return new VehicleUtilizationDto
                    {
                        VehicleId = g.Key,
                        //PlateNo = vehicle?.PlateNo ?? "",
                        PlateNo = vehicle?.VehicleMake.Name + " " + vehicle.VehicleModel.Name + " " + vehicle.PlateNo,
                        UsagePercentage = (decimal)((durationHours / totalHours) * 100),
                        IdleTime = TimeSpan.FromHours(Math.Max(0, totalHours - durationHours)),
                        Downtime = TimeSpan.Zero, // to compute: use maintenance records
                        RevenueHours = (decimal)durationHours
                    };
                }).ToList();

            return groups;
        }

        public async Task<List<VehicleComparisonDto>> GetVehicleComparisonAsync(DateTime from, DateTime to, IEnumerable<long> vehicleIds)
        {
            var branchId = _authUser.CompanyBranchId;
            var q = _db.Trips.AsNoTracking()
                .Include(t => t.Vehicle).ThenInclude(v => v.VehicleMake)
                .Include(t => t.Vehicle).ThenInclude(v => v.VehicleModel)
                .Where(t => t.CompanyBranchId == branchId && t.ActualEndDate >= from && t.ActualEndDate <= to);

            if (vehicleIds != null && vehicleIds.Any())
                q = q.Where(t => vehicleIds.Contains(t.VehicleId));

            var grouping = await q.GroupBy(t => t.VehicleId)
                .Select(g => new VehicleComparisonDto
                {
                    VehicleId = g.Key,
                    PlateNo = g.Select(x => x.Vehicle.PlateNo).FirstOrDefault(),
                    TotalDistance = g.Sum(x => (decimal?)x.ActualDistance) ?? 0,
                    FuelCost = g.Sum(x => (decimal?)x.ActualFuelCost) ?? 0,
                    MaintenanceCost = _db.MaintenanceRecords.Where(m => m.VehicleId == g.Key && m.CreatedDate >= from && m.CreatedDate <= to).Sum(m => (decimal?)m.Cost) ?? 0
                }).ToListAsync();

            return grouping;
        }

        public async Task<List<MaintenanceScheduleDto>> GetMaintenanceScheduleAsync()
        {
            var branchId = _authUser.CompanyBranchId;
            // Basic: next service date from vehicle.LastServiceDate + service interval (assuming interval stored) — otherwise use last service + recommended months
            var vehicles = await _db.Vehicles.AsNoTracking()
                .Include(v => v.VehicleMake)
                .Include(v => v.VehicleModel)
                .Where(v => v.CompanyBranchId == branchId).ToListAsync();
            var result = vehicles.Select(v => new MaintenanceScheduleDto
            {
                VehicleId = v.Id,
                PlateNo = v.VehicleMake.Name + " " + v.VehicleModel.Name + " " + v.PlateNo,
                NextMaintenanceDate = v.LastServiceDate?.AddMonths(6) ?? DateTime.UtcNow.AddMonths(1),
                ProjectedCost = _db.MaintenanceRecords.Where(m => m.VehicleId == v.Id).OrderByDescending(m => m.CreatedDate).Select(m => (decimal?)m.Cost).FirstOrDefault() ?? 0
            }).ToList();

            return result;
        }

        public async Task<List<TireManagementDto>> GetTireManagementAsync()
        {
            var branchId = _authUser.CompanyBranchId;
            // If you store tire changed date or mileage, use it; assuming MaintenanceRecords with type 'TireReplacement'
            var q = await _db.Vehicles.AsNoTracking().Where(v => v.CompanyBranchId == branchId).ToListAsync();
            var result = q.Select(v => new TireManagementDto
            {
                VehicleId = v.Id,
                PlateNo = v.PlateNo,
                KilometersSinceReplacement = v.Mileage ?? 0,
                RecommendedLifespanKm = 40000, // default; make configurable
                ReplacementCost = 100000 // default; make configurable
            }).ToList();

            return result;
        }

        public async Task<List<OvertimeAnalysisDto>> GetOvertimeAnalysisAsync(DateTime from, DateTime to)
        {
            var branchId = _authUser.CompanyBranchId;
            // Basic: compute hours > standard per trip or rely on shift logs; assume trips greater than 8h count as overtime.
            var trips = await _db.Trips.AsNoTracking().Where(t => t.CompanyBranchId == branchId && t.ActualStartDate != null && t.ActualEndDate != null && t.ActualStartDate >= from && t.ActualEndDate <= to).ToListAsync();

            var grouped = trips.GroupBy(t => t.DriverId)
                .Select(g =>
                {
                    var overtimeHours = g.Sum(x =>
                    {
                        var dur = x.ActualEndDate.Value - x.ActualStartDate.Value;
                        return Math.Max(0, dur.TotalHours - 8); // hours beyond 8 per trip
                    });

                    return new OvertimeAnalysisDto
                    {
                        DriverId = g.Key.Value,
                        DriverName = g.Select(x => x.Driver.User.FirstName + " " + x.Driver.User.LastName).FirstOrDefault(),
                        OvertimeHours = overtimeHours,
                        Cost = (decimal)overtimeHours * 2000 // default rate — make configurable
                    };
                }).ToList();

            return grouped;
        }

        public async Task<ReportSummaryViewModel> GetDashboardSummaryAsync(DateTime from, DateTime to)
        {
            var branchId = _authUser.CompanyBranchId;
            //var query = _db.Trips
            //        .AsNoTracking()
            //        .Where(t => t.CompanyBranchId == branchId && t.IsActive);

            var totalTrips = await _db.Trips.AsNoTracking().CountAsync(t => t.CompanyBranchId == branchId && t.CreatedDate >= from && t.CreatedDate <= to);
            var totalFuelCost = await _db.FuelLogs.AsNoTracking().Where(f => f.Vehicle.CompanyBranchId == branchId && f.Date >= from && f.Date <= to).SumAsync(f => (decimal?)f.Cost) ?? 0;
            var activeVehicles = await _db.Vehicles.AsNoTracking().CountAsync(v => v.CompanyBranchId == branchId && v.VehicleStatus == VehicleStatus.Active);
            var totalVehicles = await _db.Vehicles.AsNoTracking().CountAsync(v => v.CompanyBranchId == branchId);
            //var totalIncidents = await _db.Incidents.AsNoTracking().CountAsync(i => i.CompanyBranchId == branchId && i.Date >= from && i.Date <= to);

            //var trips = await query.ToListAsync();

            return new ReportSummaryViewModel
            {
                //TotalTrips = trips.Count,
                TotalTrips = totalTrips,
                TotalFuelCost = totalFuelCost,
                ActiveVehicles = activeVehicles,
                TotalVehicles = totalVehicles,
                //TotalIncidents = totalIncidents
            };
        }



        #region Helpers

        // ------------------ Cached search implementations ------------------

        public async Task<PaginatedResult<SelectListItem>> SearchDriversAsync(string q, int page = 1, int pageSize = 20)
        {
            if (!_authUser.CompanyBranchId.HasValue)
                throw new InvalidOperationException("Missing CompanyBranchId for current user.");

            var branchId = _authUser.CompanyBranchId.Value;
            var cacheKey = GetDriverCacheKey(branchId, q, page, pageSize);

            return await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = SearchCacheDuration;
                // optional sliding expiration:
                entry.SlidingExpiration = TimeSpan.FromMinutes(2);

                // Query DB
                var query = _db.Drivers.AsNoTracking()
                    .Where(d => d.CompanyBranchId == branchId)
                    .Select(d => new SelectListItem
                    {
                        Value = d.Id.ToString(),
                        Text = ((d.User.FirstName ?? "") + " " + (d.User.LastName ?? "")).Trim()
                    });

                if (!string.IsNullOrWhiteSpace(q))
                {
                    var s = q.Trim().ToLower();
                    query = query.Where(x => x.Text.ToLower().Contains(s));
                }

                var ordered = query.OrderBy(x => x.Text);
                return await PaginatedResult<SelectListItem>.CreateAsync(ordered, page, pageSize);
            });
        }

        public async Task<PaginatedResult<SelectListItem>> SearchVehiclesAsync(string q, int page = 1, int pageSize = 20)
        {
            if (!_authUser.CompanyBranchId.HasValue)
                throw new InvalidOperationException("Missing CompanyBranchId for current user.");

            var branchId = _authUser.CompanyBranchId.Value;

            // 1) Build DB query and apply DB-translatable filtering
            var baseQ = _db.Vehicles.AsNoTracking()
                .Where(v => v.CompanyBranchId == branchId);

            if (!string.IsNullOrWhiteSpace(q))
            {
                var pattern = $"%{q.Trim()}%";
                baseQ = baseQ.Where(v =>
                    EF.Functions.Like(v.PlateNo ?? "", pattern)
                    || EF.Functions.Like(v.VehicleMake!.Name ?? "", pattern)
                    || EF.Functions.Like(v.VehicleModel!.Name ?? "", pattern)
                );
            }

            // 2) Project only DB fields we need for ordering/paging (no string.Format / complex string ops here)
            var projected = baseQ
                .Select(v => new
                {
                    v.Id,
                    PlateNo = v.PlateNo,
                    MakeName = v.VehicleMake != null ? v.VehicleMake.Name : "",
                    ModelName = v.VehicleModel != null ? v.VehicleModel.Name : ""
                });

            // 3) Order by DB columns (translatable)
            var ordered = projected
                .OrderBy(x => x.MakeName)
                .ThenBy(x => x.ModelName)
                .ThenBy(x => x.PlateNo);

            // 4) Get total count (for pagination metadata)
            var totalCount = await ordered.CountAsync();

            // 5) Apply Skip/Take and materialize page (ToListAsync) — now we are on the client side
            var pageItems = await ordered
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // 6) Build the label strings in memory (safe — not part of EF expression)
            var mapped = pageItems.Select(x =>
            {
                var makeModel = string.Join(" ", new[] { (x.MakeName ?? "").Trim(), (x.ModelName ?? "").Trim() }
                                              .Where(s => !string.IsNullOrWhiteSpace(s))).Trim();

                string label;
                if (string.IsNullOrWhiteSpace(makeModel))
                    label = string.IsNullOrWhiteSpace(x.PlateNo) ? $"Vehicle {x.Id}" : x.PlateNo ?? $"Vehicle {x.Id}";
                else
                    label = makeModel + (string.IsNullOrWhiteSpace(x.PlateNo) ? "" : $" ({x.PlateNo})");

                return new SelectListItem
                {
                    Value = x.Id.ToString(),
                    Text = label
                };
            }).ToList();

            // 7) Return a PaginatedResult<SelectListItem> (constructed manually)
            var result = new PaginatedResult<SelectListItem>
            {
                Items = mapped,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };

            return result;
        }

        public async Task<SelectListItem?> GetDriverByIdAsync(long id)
        {
            if (!_authUser.CompanyBranchId.HasValue) return null;
            var branchId = _authUser.CompanyBranchId.Value;
            var cacheKey = GetDriverByIdCacheKey(branchId, id);

            return await _cache.GetOrCreateAsync<SelectListItem?>(cacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = SingleItemCacheDuration;
                var item = await _db.Drivers.AsNoTracking()
                    .Where(x => x.Id == id && x.CompanyBranchId == branchId)
                    .Select(x => new SelectListItem
                    {
                        Value = x.Id.ToString(),
                        Text = ((x.User.FirstName ?? "") + " " + (x.User.LastName ?? "")).Trim()
                    }).FirstOrDefaultAsync();
                return item;
            });
        }

        public async Task<SelectListItem?> GetVehicleByIdAsync(long id)
        {
            if (!_authUser.CompanyBranchId.HasValue) return null;
            var branchId = _authUser.CompanyBranchId.Value;

            var v = await _db.Vehicles.AsNoTracking()
                .Where(x => x.Id == id && x.CompanyBranchId == branchId)
                .Select(x => new
                {
                    x.Id,
                    PlateNo = x.PlateNo,
                    Make = x.VehicleMake != null ? x.VehicleMake.Name : null,
                    Model = x.VehicleModel != null ? x.VehicleModel.Name : null
                })
                .FirstOrDefaultAsync();

            if (v == null) return null;

            // Build a clean "Make Model" string (skip null/empty parts)
            var make = (v.Make ?? "").Trim();
            var model = (v.Model ?? "").Trim();
            var makeModel = string.Join(" ", new[] { make, model }.Where(s => !string.IsNullOrWhiteSpace(s))).Trim();

            string label;
            if (string.IsNullOrWhiteSpace(makeModel))
            {
                // No make/model, fall back to plate or vehicle id
                label = string.IsNullOrWhiteSpace(v.PlateNo) ? $"Vehicle {v.Id}" : v.PlateNo;
            }
            else
            {
                // Have make/model — append plate in parentheses if present
                label = makeModel + (string.IsNullOrWhiteSpace(v.PlateNo) ? "" : $" ({v.PlateNo})");
            }

            return new SelectListItem { Value = v.Id.ToString(), Text = label };
        }

        // ------------- Cache invalidation helpers --------------
        public Task InvalidateDriverCacheAsync(long? branchId = null)
        {
            if (!branchId.HasValue)
            {
                // if no branch provided, try current user's branch
                branchId = _authUser.CompanyBranchId;
            }
            if (!branchId.HasValue) return Task.CompletedTask;

            var versionKey = DriverVersionKey(branchId.Value);
            BumpBranchVersion(versionKey);
            return Task.CompletedTask;
        }

        public Task InvalidateVehicleCacheAsync(long? branchId = null)
        {
            if (!branchId.HasValue)
            {
                branchId = _authUser.CompanyBranchId;
            }
            if (!branchId.HasValue) return Task.CompletedTask;

            var versionKey = VehicleVersionKey(branchId.Value);
            BumpBranchVersion(versionKey);
            return Task.CompletedTask;
        }


        #endregion

    }

}
