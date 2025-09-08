using FleetManager.Business.Database.Entities.MaintenanceTicket;
using FleetManager.Business.Database.Entities;
using FleetManager.Business.DataObjects.AdminDashboardDto;
using FleetManager.Business.DataObjects.CompanyOwnerDashboardDto;
using FleetManager.Business.Enums;
using FleetManager.Business.Interfaces.AdminDashboardModule;
using FleetManager.Business.Interfaces.CompanyDashboardModule;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Implementations.CompanyDashboardModule
{

    public class CompanyOwnerDashboardService : ICompanyOwnerDashboardService
    {
        private readonly FleetManagerDbContext _db;
        private readonly IMemoryCache _cache;
        private readonly ILogger<CompanyOwnerDashboardService> _logger;
        private readonly IAdminDashboardService _adminDashboard;
        private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(20);

        public CompanyOwnerDashboardService(
            FleetManagerDbContext db,
            IMemoryCache cache,
            ILogger<CompanyOwnerDashboardService> logger,
            IAdminDashboardService adminDashboard)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger;
            _adminDashboard = adminDashboard;
        }

        private record BranchDriverStats(long BranchId, int Total, int Active);
        private record BranchVehicleStats(long BranchId, int Total, int Active);
        private record BranchTicketStats(long BranchId, int Open, int Overdue);

        private (DateTime fromDate, DateTime toDate) ResolveRange(DashboardRequestDto req)
        {
            var toDate = (req.DateTo ?? DateTimeOffset.UtcNow).UtcDateTime;
            var fromDate = (req.DateFrom ?? DateTimeOffset.UtcNow.AddMonths(-6)).UtcDateTime;
            return (fromDate, toDate);
        }

        public async Task<CompanyOwnerDashboardDto> GetCompanyOwnerDashboardAsync(DashboardRequestDto req, CancellationToken ct = default)
        {
            if (req.CompanyId == null) throw new ArgumentException("CompanyId required", nameof(req.CompanyId));
            var (fromDate, toDate) = ResolveRange(req);
            var companyId = req.CompanyId.Value;
            var branchFilter = req.CompanyBranchId;
            var cacheKey = $"CompanyOwnerDashboard:Company:{companyId}:Branch:{branchFilter?.ToString() ?? "All"}:From:{fromDate:yyyyMMdd}:To:{toDate:yyyyMMdd}";

            if (_cache.TryGetValue(cacheKey, out CompanyOwnerDashboardDto cached))
            {
                cached.CacheHit = true;
                return cached;
            }

            var dto = new CompanyOwnerDashboardDto
            {
                CompanyId = companyId,
                GeneratedAt = DateTimeOffset.UtcNow,
                TimeZoneId = req.TimeZoneId ?? "UTC",
                CacheHit = false,
                Totals = new TotalsDto(),
                IsFiltered = branchFilter.HasValue // Add this flag
            };

            // Get all branches for the dropdown (always fetch all company branches)
            var branchesAll = await _db.Set<CompanyBranch>().AsNoTracking()
                .Where(b => b.CompanyId == companyId)
                .Select(b => new { b.Id, b.Name, b.ManagerName })
                .OrderBy(b => b.Name) // Add ordering for consistency
                .ToListAsync(ct);

            // Determine which branches to include in the summary (either all or the single selected branch)
            var branchIds = branchFilter.HasValue ? new List<long> { branchFilter.Value } : branchesAll.Select(x => x.Id).ToList();

            // Validate that the filtered branch exists and belongs to the company
            if (branchFilter.HasValue && !branchesAll.Any(b => b.Id == branchFilter.Value))
            {
                throw new KeyNotFoundException($"Branch {branchFilter.Value} not found or doesn't belong to company {companyId}");
            }

            // basic counts - either company-wide or branch-scoped (counts reflect the scope)
            if (branchFilter.HasValue)
            {
                var bId = branchFilter.Value;
                dto.BranchCount = await _db.Set<CompanyBranch>().AsNoTracking().CountAsync(b => b.CompanyId == companyId && b.Id == bId, ct);
                dto.AdminCount = await _db.Set<CompanyAdmin>().AsNoTracking().CountAsync(a => a.CompanyId == companyId && a.CompanyBranchId == bId, ct);
                dto.VehicleCount = await _db.Set<Vehicle>().AsNoTracking().CountAsync(v => v.CompanyBranchId == bId && v.CompanyId == companyId, ct);
                dto.DriverCount = await _db.Set<Driver>().AsNoTracking().CountAsync(d => d.CompanyBranchId == bId && d.CompanyId == companyId, ct);
            }
            else
            {
                dto.BranchCount = branchesAll.Count;
                dto.AdminCount = await _db.Set<CompanyAdmin>().AsNoTracking().CountAsync(a => a.CompanyId == companyId, ct);
                dto.VehicleCount = await _db.Set<Vehicle>().AsNoTracking().CountAsync(v => v.CompanyId == companyId, ct);
                dto.DriverCount = await _db.Set<Driver>().AsNoTracking().CountAsync(d => d.CompanyId == companyId, ct);
            }

            // vehicle status distribution (pie chart) - respect branch filter
            var vehicleStatusesQuery = _db.Set<Vehicle>().AsNoTracking()
                .Where(v => v.CompanyId == companyId && v.CompanyBranchId != null && branchIds.Contains(v.CompanyBranchId.Value));

            var vehicleStatuses = await vehicleStatusesQuery
                .GroupBy(v => v.VehicleStatus)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync(ct);

            // assigned vehicle count company-wide or branch-scoped (distinct vehicles assigned)
            var assignedVehicleQuery = from dv in _db.Set<DriverVehicle>().AsNoTracking()
                                       join v in _db.Set<Vehicle>().AsNoTracking() on dv.VehicleId equals v.Id
                                       where v.CompanyId == companyId && v.CompanyBranchId != null && branchIds.Contains(v.CompanyBranchId.Value)
                                       select dv.VehicleId;

            var assignedVehicleCount = await assignedVehicleQuery.Distinct().CountAsync(ct);

            dto.VehicleStatusDistribution = new Dictionary<string, int>();
            foreach (var status in vehicleStatuses)
            {
                dto.VehicleStatusDistribution[status.Status.ToString()] = status.Count;
            }

            var totalVehicles = vehicleStatuses.Sum(s => s.Count);
            var unassignedCount = totalVehicles - assignedVehicleCount;
            if (unassignedCount > 0)
            {
                dto.VehicleStatusDistribution["Unassigned"] = unassignedCount;
            }

            // drivers grouped -> BranchDriverStats (filtered by branchIds)
            var driversGrouped = await _db.Set<Driver>().AsNoTracking()
                .Where(d => d.CompanyId == companyId && d.CompanyBranchId != null && branchIds.Contains(d.CompanyBranchId.Value))
                .GroupBy(d => d.CompanyBranchId)
                .Select(g => new BranchDriverStats(g.Key.Value, g.Count(), g.Count(d => d.IsActive)))
                .ToListAsync(ct);
            var driversMap = driversGrouped.ToDictionary(x => x.BranchId, x => x);

            // vehicles grouped -> BranchVehicleStats
            var vehiclesGrouped = await _db.Set<Vehicle>().AsNoTracking()
                .Where(v => v.CompanyId == companyId && v.CompanyBranchId != null && branchIds.Contains(v.CompanyBranchId.Value))
                .GroupBy(v => v.CompanyBranchId)
                .Select(g => new BranchVehicleStats(g.Key.Value, g.Count(), g.Count(v => v.VehicleStatus == VehicleStatus.Active)))
                .ToListAsync(ct);
            var vehiclesMap = vehiclesGrouped.ToDictionary(x => x.BranchId, x => x);

            // assigned vehicles grouped per branch
            var assignedGrouped = await (from dv in _db.Set<DriverVehicle>().AsNoTracking()
                                         join v in _db.Set<Vehicle>().AsNoTracking() on dv.VehicleId equals v.Id
                                         where v.CompanyId == companyId && v.CompanyBranchId != null && branchIds.Contains(v.CompanyBranchId.Value)
                                         group dv by v.CompanyBranchId into g
                                         select new { BranchId = g.Key.Value, Assigned = g.Select(x => x.VehicleId).Distinct().Count() })
                                        .ToListAsync(ct);
            var assignedMap = assignedGrouped.ToDictionary(x => x.BranchId, x => x.Assigned);

            // tickets grouped (range) - respect branchIds and date range
            var (fromD, toD) = ResolveRange(req);
            var ticketsGrouped = await _db.Set<MaintenanceTicket>().AsNoTracking()
                .Where(t => t.CompanyBranch.CompanyId == companyId
                            && t.CompanyBranchId != null
                            && branchIds.Contains(t.CompanyBranchId.Value)
                            && t.CreatedDate >= fromD && t.CreatedDate <= toD)
                .GroupBy(t => t.CompanyBranchId)
                .Select(g => new BranchTicketStats(
                     g.Key.Value,
                     g.Count(t => t.Status != TicketStatus.Resolved && t.Status != TicketStatus.Rejected),
                     g.Count(t => t.Priority == MaintenancePriority.High && t.Status == TicketStatus.Pending && t.CreatedDate <= DateTime.UtcNow.AddDays(-7))
                ))
                .ToListAsync(ct);
            var ticketsMap = ticketsGrouped.ToDictionary(x => x.BranchId, x => x);

            // vendors grouped
            var vendorsGrouped = await _db.Set<ContactDirectory>().AsNoTracking()
                .Where(c => c.CompanyBranch.CompanyId == companyId && c.CompanyBranchId != null && branchIds.Contains(c.CompanyBranchId.Value))
                .GroupBy(c => c.CompanyBranchId)
                .Select(g => new { BranchId = g.Key.Value, Count = g.Count() })
                .ToListAsync(ct);
            var vendorsMap = vendorsGrouped.ToDictionary(x => x.BranchId, x => x.Count);

            // financials grouped (fuel, invoice, fines)
            var fuelGrouped = await (from f in _db.Set<FuelLog>().AsNoTracking()
                                     join v in _db.Set<Vehicle>().AsNoTracking() on f.VehicleId equals v.Id
                                     where v.CompanyId == companyId
                                           && v.CompanyBranchId != null
                                           && branchIds.Contains(v.CompanyBranchId.Value)
                                           && f.Date >= fromD && f.Date <= toD
                                     group f by v.CompanyBranchId into g
                                     select new { BranchId = g.Key.Value, FuelSpend = g.Sum(x => (decimal?)x.Cost) ?? 0m })
                                    .ToListAsync(ct);
            var fuelMap = fuelGrouped.ToDictionary(x => x.BranchId, x => x.FuelSpend);

            var maintGrouped = await _db.Set<Invoice>().AsNoTracking()
                .Where(i => i.CompanyBranch.CompanyId == companyId && i.CompanyBranchId != null && branchIds.Contains(i.CompanyBranchId.Value) && i.InvoiceDate >= fromD && i.InvoiceDate <= toD)
                .GroupBy(i => i.CompanyBranchId)
                .Select(g => new { BranchId = g.Key.Value, Spend = g.Sum(x => (decimal?)x.TotalAmount) ?? 0m })
                .ToListAsync(ct);
            var maintMap = maintGrouped.ToDictionary(x => x.BranchId, x => x.Spend);

            var finesGrouped = await _db.Set<FineAndToll>().AsNoTracking()
                .Where(f => f.CompanyBranch.CompanyId == companyId && f.CompanyBranchId != null && branchIds.Contains(f.CompanyBranchId.Value) && f.PaidDate != null && f.PaidDate >= fromD && f.PaidDate <= toD)
                .GroupBy(f => f.CompanyBranchId)
                .Select(g => new { BranchId = g.Key.Value, Spend = g.Sum(x => (decimal?)x.Amount) ?? 0m })
                .ToListAsync(ct);
            var finesMap = finesGrouped.ToDictionary(x => x.BranchId, x => x.Spend);

            // company admin lookup (for branches in scope)
            var adminMap = await _db.Set<CompanyAdmin>().AsNoTracking()
                .Where(ca => ca.CompanyId == companyId && ca.CompanyBranchId != null && branchIds.Contains(ca.CompanyBranchId.Value))
                .Select(ca => new { BranchId = ca.CompanyBranchId.Value, Name = ca.User.FirstName + " " + ca.User.LastName })
                .ToDictionaryAsync(x => x.BranchId, x => x.Name, ct);

            // build BranchSummaryDto list for branches in scope (either all or single branch)
            var branchSummaries = new List<BranchSummaryDto>();
            foreach (var b in branchesAll.Where(b => branchIds.Contains(b.Id)))
            {
                // Use typed records and default instances when missing
                driversMap.TryGetValue(b.Id, out var dg);
                dg ??= new BranchDriverStats(b.Id, 0, 0);

                vehiclesMap.TryGetValue(b.Id, out var vg);
                vg ??= new BranchVehicleStats(b.Id, 0, 0);

                assignedMap.TryGetValue(b.Id, out var assignedVehicles);
                ticketsMap.TryGetValue(b.Id, out var tk);
                tk ??= new BranchTicketStats(b.Id, 0, 0);

                var fuelSpend = fuelMap.TryGetValue(b.Id, out var fs) ? fs : 0m;
                var maintSpend = maintMap.TryGetValue(b.Id, out var ms) ? ms : 0m;
                var finesSpend = finesMap.TryGetValue(b.Id, out var fs2) ? fs2 : 0m;

                var bs = new BranchSummaryDto
                {
                    BranchId = b.Id,
                    BranchName = b.Name,
                    ManagerName = b.ManagerName,
                    TotalDrivers = dg.Total,
                    ActiveDrivers = dg.Active,
                    TotalVehicles = vg.Total,
                    ActiveVehicles = vg.Active,
                    AssignedVehicleCount = assignedVehicles,
                    OpenMaintenanceTickets = tk.Open,
                    OverdueMaintenanceTickets = tk.Overdue,
                    VendorsCount = vendorsMap.TryGetValue(b.Id, out var vc) ? vc : 0,
                    FuelSpend = fuelSpend,
                    MaintenanceSpend = maintSpend,
                    FinesSpend = finesSpend,
                    CompanyAdminName = adminMap.TryGetValue(b.Id, out var nm) ? nm : null,
                    TotalSpend = fuelSpend + maintSpend + finesSpend
                };

                // Performance calculation
                if (vg.Total > 0)
                {
                    var utilizationRate = (double)assignedVehicles / vg.Total;
                    var expensePerVehicle = vg.Total > 0 ? (double)bs.TotalSpend / vg.Total : 0;
                    var expenseEfficiency = expensePerVehicle > 0 ? Math.Max(0, 100 - (expensePerVehicle / 10000)) : 100;
                    bs.PerformancePercentage = Math.Min(100, (utilizationRate * 50) + (expenseEfficiency * 0.5));
                }
                else
                {
                    bs.PerformancePercentage = 0;
                }

                branchSummaries.Add(bs);
            }

            dto.Branches = branchSummaries;

            // Calculate totals (these now reflect the scope)
            dto.Totals.TotalDrivers = dto.DriverCount;
            dto.Totals.TotalVehicles = dto.VehicleCount;
            dto.Totals.AssignedVehicles = assignedVehicleCount;
            dto.Totals.FuelSpend = branchSummaries.Sum(x => x.FuelSpend);
            dto.Totals.MaintenanceSpend = branchSummaries.Sum(x => x.MaintenanceSpend);
            dto.Totals.FinesSpend = branchSummaries.Sum(x => x.FinesSpend);
            dto.Totals.TotalSpend = dto.Totals.FuelSpend + dto.Totals.MaintenanceSpend + dto.Totals.FinesSpend;

            // Keep branches list for dropdown (all branches)
            dto.AllBranches = branchesAll.Select(b => new BranchListItemDto { BranchId = b.Id, BranchName = b.Name }).ToList();

            _cache.Set(cacheKey, dto, CacheTtl);
            return dto;
        }

        public async Task<BranchDetailDto> GetBranchDetailsAsync(long branchId, DashboardRequestDto req, CancellationToken ct = default)
        {
            // ensure branch exists and belongs to company (validate CompanyId from req)
            var branch = await _db.Set<CompanyBranch>().AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == branchId && b.CompanyId == req.CompanyId, ct) // Add company validation
                ?? throw new KeyNotFoundException("Branch not found or doesn't belong to company");

            // create a minimal request for admin service calls
            var adminReq = new DashboardRequestDto
            {
                CompanyBranchId = branchId,
                DateFrom = req.DateFrom,
                DateTo = req.DateTo,
                RecentListSize = Math.Max(1, req.RecentListSize)
            };

            var detail = new BranchDetailDto
            {
                BranchId = branch.Id,
                BranchName = branch.Name,
                ManagerName = branch.ManagerName,
                Summary = (await GetCompanyOwnerDashboardAsync(new DashboardRequestDto
                {
                    CompanyId = req.CompanyId,
                    CompanyBranchId = branchId,
                    DateFrom = req.DateFrom,
                    DateTo = req.DateTo,
                    RecentListSize = Math.Max(1, req.RecentListSize)
                }, ct)).Branches.FirstOrDefault(b => b.BranchId == branchId) // reuse
            };

            // Reuse AdminDashboardService to get branch-specific small lists & charts
            detail.RecentFuelLogs = await _adminDashboard.GetRecentFuelLogsAsync(adminReq, ct);
            detail.RecentMaintenanceTickets = await _adminDashboard.GetRecentMaintenanceTicketsAsync(adminReq, ct);
            detail.TopVehiclesByFuel = await _adminDashboard.GetTopVehiclesByFuelAsync(adminReq, 10, ct);
            detail.ExpensesByMonth = await _adminDashboard.GetMaintenanceByMonthAsync(adminReq, ct); // maintenance chart
            return detail;
        }

        public async Task<List<MonthPointDto>> GetCompanyExpensesByMonthAsync(DashboardRequestDto req, CancellationToken ct = default)
        {
            var (fromDate, toDate) = ResolveRange(req);
            var companyId = req.CompanyId ?? throw new ArgumentException("CompanyId required", nameof(req.CompanyId));
            var branchFilter = req.CompanyBranchId;
            var cacheKey = $"CompanyExpensesByMonth:Company:{companyId}:Branch:{branchFilter?.ToString() ?? "All"}:From:{fromDate:yyyyMMdd}:To:{toDate:yyyyMMdd}";
            if (_cache.TryGetValue(cacheKey, out List<MonthPointDto> cached)) return cached;

            // Validate branch belongs to company if specified
            if (branchFilter.HasValue)
            {
                var branchExists = await _db.Set<CompanyBranch>().AsNoTracking()
                    .AnyAsync(b => b.Id == branchFilter.Value && b.CompanyId == companyId, ct);
                if (!branchExists)
                {
                    throw new KeyNotFoundException($"Branch {branchFilter.Value} not found or doesn't belong to company {companyId}");
                }
            }

            // Combine fuel (cost) + maintenance (invoice total) + fines (paid) per month
            var fuelQ = from f in _db.Set<FuelLog>().AsNoTracking()
                        join v in _db.Set<Vehicle>().AsNoTracking() on f.VehicleId equals v.Id
                        where v.CompanyId == companyId
                              && f.Date >= fromDate && f.Date <= toDate
                              && (!branchFilter.HasValue || (v.CompanyBranchId != null && v.CompanyBranchId == branchFilter.Value))
                        group f by new { f.Date.Year, f.Date.Month } into g
                        select new { g.Key.Year, g.Key.Month, Fuel = g.Sum(x => (decimal?)x.Cost) ?? 0m };

            var maintQ = from i in _db.Set<Invoice>().AsNoTracking()
                         where i.CompanyBranch.CompanyId == companyId
                               && i.InvoiceDate >= fromDate && i.InvoiceDate <= toDate
                               && (!branchFilter.HasValue || (i.CompanyBranchId != null && i.CompanyBranchId == branchFilter.Value))
                         group i by new { i.InvoiceDate.Year, i.InvoiceDate.Month } into g
                         select new { g.Key.Year, g.Key.Month, Maint = g.Sum(x => (decimal?)x.TotalAmount) ?? 0m };

            var finesQ = from f in _db.Set<FineAndToll>().AsNoTracking()
                         where f.CompanyBranch.CompanyId == companyId && f.PaidDate != null && f.PaidDate >= fromDate && f.PaidDate <= toDate
                               && (!branchFilter.HasValue || (f.CompanyBranchId != null && f.CompanyBranchId == branchFilter.Value))
                         group f by new { f.PaidDate.Value.Year, f.PaidDate.Value.Month } into g
                         select new { Year = g.Key.Year, Month = g.Key.Month, Fines = g.Sum(x => (decimal?)x.Amount) ?? 0m };

            var fuelList = await fuelQ.ToListAsync(ct);
            var maintList = await maintQ.ToListAsync(ct);
            var finesList = await finesQ.ToListAsync(ct);

            var keys = fuelList.Select(x => (x.Year, x.Month))
                        .Union(maintList.Select(x => (x.Year, x.Month)))
                        .Union(finesList.Select(x => (x.Year, x.Month)))
                        .Distinct();

            var result = keys.Select(k => new MonthPointDto
            {
                Year = k.Year,
                Month = k.Month,
                Value = (fuelList.FirstOrDefault(x => x.Year == k.Year && x.Month == k.Month)?.Fuel ?? 0m)
                        + (maintList.FirstOrDefault(x => x.Year == k.Year && x.Month == k.Month)?.Maint ?? 0m)
                        + (finesList.FirstOrDefault(x => x.Year == k.Year && x.Month == k.Month)?.Fines ?? 0m),
                SecondaryValue = fuelList.FirstOrDefault(x => x.Year == k.Year && x.Month == k.Month)?.Fuel ?? 0m
            }).OrderBy(p => p.Year).ThenBy(p => p.Month).ToList();

            _cache.Set(cacheKey, result, CacheTtl);
            return result;
        }
    }

    #region ChatGpt
    //public class CompanyOwnerDashboardService : ICompanyOwnerDashboardService
    //{
    //    private readonly FleetManagerDbContext _db;
    //    private readonly IMemoryCache _cache;
    //    private readonly ILogger<CompanyOwnerDashboardService> _logger;
    //    private readonly IAdminDashboardService _adminDashboard; // reuse existing granular methods
    //    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(20);

    //    public CompanyOwnerDashboardService(
    //        FleetManagerDbContext db,
    //        IMemoryCache cache,
    //        ILogger<CompanyOwnerDashboardService> logger,
    //        IAdminDashboardService adminDashboard)
    //    {
    //        _db = db ?? throw new ArgumentNullException(nameof(db));
    //        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    //        _logger = logger;
    //        _adminDashboard = adminDashboard;
    //    }

    //    private record BranchDriverStats(long BranchId, int Total, int Active);
    //    private record BranchVehicleStats(long BranchId, int Total, int Active);
    //    private record BranchTicketStats(long BranchId, int Open, int Overdue);

    //    private (DateTime fromDate, DateTime toDate) ResolveRange(DashboardRequestDto req)
    //    {
    //        var toDate = (req.DateTo ?? DateTimeOffset.UtcNow).UtcDateTime;
    //        var fromDate = (req.DateFrom ?? DateTimeOffset.UtcNow.AddMonths(-6)).UtcDateTime;
    //        return (fromDate, toDate);
    //    }

    //    public async Task<CompanyOwnerDashboardDto> GetCompanyOwnerDashboardAsync(DashboardRequestDto req, CancellationToken ct = default)
    //    {
    //        if (req.CompanyId == null) throw new ArgumentException("CompanyId required", nameof(req.CompanyId));
    //        var (fromDate, toDate) = ResolveRange(req);
    //        var companyId = req.CompanyId.Value;
    //        var branchFilter = req.CompanyBranchId;
    //        var cacheKey = $"CompanyOwnerDashboard:Company:{companyId}:Branch:{branchFilter?.ToString() ?? "All"}:From:{fromDate:yyyyMMdd}:To:{toDate:yyyyMMdd}";

    //        if (_cache.TryGetValue(cacheKey, out CompanyOwnerDashboardDto cached))
    //        {
    //            cached.CacheHit = true;
    //            return cached;
    //        }

    //        var dto = new CompanyOwnerDashboardDto
    //        {
    //            CompanyId = companyId,
    //            GeneratedAt = DateTimeOffset.UtcNow,
    //            TimeZoneId = req.TimeZoneId ?? "UTC",
    //            CacheHit = false,
    //            Totals = new TotalsDto()
    //        };

    //        // Get all branches for the dropdown (always fetch all company branches)
    //        var branchesAll = await _db.Set<CompanyBranch>().AsNoTracking()
    //            .Where(b => b.CompanyId == companyId)
    //            .Select(b => new { b.Id, b.Name, b.ManagerName })
    //            .ToListAsync(ct);

    //        // Determine which branches to include in the summary (either all or the single selected branch)
    //        var branchIds = branchFilter.HasValue ? new List<long> { branchFilter.Value } : branchesAll.Select(x => x.Id).ToList();

    //        // basic counts - either company-wide or branch-scoped (counts reflect the scope)
    //        if (branchFilter.HasValue)
    //        {
    //            var bId = branchFilter.Value;
    //            dto.BranchCount = await _db.Set<CompanyBranch>().AsNoTracking().CountAsync(b => b.CompanyId == companyId && b.Id == bId, ct);
    //            dto.AdminCount = await _db.Set<CompanyAdmin>().AsNoTracking().CountAsync(a => a.CompanyId == companyId && a.CompanyBranchId == bId, ct);

    //            dto.VehicleCount = await _db.Set<Vehicle>().AsNoTracking().CountAsync(v => v.CompanyBranchId == bId && v.CompanyId == companyId, ct);
    //            dto.DriverCount = await _db.Set<Driver>().AsNoTracking().CountAsync(d => d.CompanyBranchId == bId && d.CompanyId == companyId, ct);
    //        }
    //        else
    //        {
    //            dto.BranchCount = branchesAll.Count;
    //            dto.AdminCount = await _db.Set<CompanyAdmin>().AsNoTracking().CountAsync(a => a.CompanyId == companyId, ct);

    //            dto.VehicleCount = await _db.Set<Vehicle>().AsNoTracking().CountAsync(v => v.CompanyId == companyId, ct);
    //            dto.DriverCount = await _db.Set<Driver>().AsNoTracking().CountAsync(d => d.CompanyId == companyId, ct);
    //        }

    //        // vehicle status distribution (pie chart) - respect branch filter
    //        var vehicleStatusesQuery = _db.Set<Vehicle>().AsNoTracking()
    //            .Where(v => v.CompanyId == companyId && v.CompanyBranchId != null && branchIds.Contains(v.CompanyBranchId.Value));

    //        var vehicleStatuses = await vehicleStatusesQuery
    //            .GroupBy(v => v.VehicleStatus)
    //            .Select(g => new { Status = g.Key, Count = g.Count() })
    //            .ToListAsync(ct);

    //        // assigned vehicle count company-wide or branch-scoped (distinct vehicles assigned)
    //        var assignedVehicleQuery = from dv in _db.Set<DriverVehicle>().AsNoTracking()
    //                                   join v in _db.Set<Vehicle>().AsNoTracking() on dv.VehicleId equals v.Id
    //                                   where v.CompanyId == companyId && v.CompanyBranchId != null && branchIds.Contains(v.CompanyBranchId.Value)
    //                                   select dv.VehicleId;

    //        var assignedVehicleCount = await assignedVehicleQuery.Distinct().CountAsync(ct);

    //        dto.VehicleStatusDistribution = new Dictionary<string, int>();
    //        foreach (var status in vehicleStatuses)
    //        {
    //            dto.VehicleStatusDistribution[status.Status.ToString()] = status.Count;
    //        }

    //        var totalVehicles = vehicleStatuses.Sum(s => s.Count);
    //        var unassignedCount = totalVehicles - assignedVehicleCount;
    //        if (unassignedCount > 0)
    //        {
    //            dto.VehicleStatusDistribution["Unassigned"] = unassignedCount;
    //        }

    //        // branches for dropdown (keep all for selection)
    //        var branches = branchesAll.Select(b => new { b.Id, b.Name, b.ManagerName }).ToList();

    //        // drivers grouped -> BranchDriverStats (filtered by branchIds)
    //        var driversGrouped = await _db.Set<Driver>().AsNoTracking()
    //            .Where(d => d.CompanyId == companyId && d.CompanyBranchId != null && branchIds.Contains(d.CompanyBranchId.Value))
    //            .GroupBy(d => d.CompanyBranchId)
    //            .Select(g => new BranchDriverStats(g.Key.Value, g.Count(), g.Count(d => d.IsActive)))
    //            .ToListAsync(ct);
    //        var driversMap = driversGrouped.ToDictionary(x => x.BranchId, x => x);

    //        // vehicles grouped -> BranchVehicleStats
    //        var vehiclesGrouped = await _db.Set<Vehicle>().AsNoTracking()
    //            .Where(v => v.CompanyId == companyId && v.CompanyBranchId != null && branchIds.Contains(v.CompanyBranchId.Value))
    //            .GroupBy(v => v.CompanyBranchId)
    //            .Select(g => new BranchVehicleStats(g.Key.Value, g.Count(), g.Count(v => v.VehicleStatus == VehicleStatus.Active)))
    //            .ToListAsync(ct);
    //        var vehiclesMap = vehiclesGrouped.ToDictionary(x => x.BranchId, x => x);

    //        // assigned vehicles grouped per branch
    //        var assignedGrouped = await (from dv in _db.Set<DriverVehicle>().AsNoTracking()
    //                                     join v in _db.Set<Vehicle>().AsNoTracking() on dv.VehicleId equals v.Id
    //                                     where v.CompanyId == companyId && v.CompanyBranchId != null && branchIds.Contains(v.CompanyBranchId.Value)
    //                                     group dv by v.CompanyBranchId into g
    //                                     select new { BranchId = g.Key.Value, Assigned = g.Select(x => x.VehicleId).Distinct().Count() })
    //                                    .ToListAsync(ct);
    //        var assignedMap = assignedGrouped.ToDictionary(x => x.BranchId, x => x.Assigned);

    //        // tickets grouped (range) - respect branchIds and date range
    //        var (fromD, toD) = ResolveRange(req);
    //        var ticketsGrouped = await _db.Set<MaintenanceTicket>().AsNoTracking()
    //            .Where(t => t.CompanyBranch.CompanyId == companyId
    //                        && t.CompanyBranchId != null
    //                        && branchIds.Contains(t.CompanyBranchId.Value)
    //                        && t.CreatedDate >= fromD && t.CreatedDate <= toD)
    //            .GroupBy(t => t.CompanyBranchId)
    //            .Select(g => new BranchTicketStats(
    //                 g.Key.Value,
    //                 g.Count(t => t.Status != TicketStatus.Resolved && t.Status != TicketStatus.Rejected),
    //                 g.Count(t => t.Priority == MaintenancePriority.High && t.Status == TicketStatus.Pending && t.CreatedDate <= DateTime.UtcNow.AddDays(-7))
    //            ))
    //            .ToListAsync(ct);
    //        var ticketsMap = ticketsGrouped.ToDictionary(x => x.BranchId, x => x);

    //        // vendors grouped
    //        var vendorsGrouped = await _db.Set<ContactDirectory>().AsNoTracking()
    //            .Where(c => c.CompanyBranch.CompanyId == companyId && c.CompanyBranchId != null && branchIds.Contains(c.CompanyBranchId.Value))
    //            .GroupBy(c => c.CompanyBranchId)
    //            .Select(g => new { BranchId = g.Key.Value, Count = g.Count() })
    //            .ToListAsync(ct);
    //        var vendorsMap = vendorsGrouped.ToDictionary(x => x.BranchId, x => x.Count);

    //        // financials grouped (fuel, invoice, fines)
    //        var fuelGrouped = await (from f in _db.Set<FuelLog>().AsNoTracking()
    //                                 join v in _db.Set<Vehicle>().AsNoTracking() on f.VehicleId equals v.Id
    //                                 where v.CompanyId == companyId
    //                                       && v.CompanyBranchId != null
    //                                       && branchIds.Contains(v.CompanyBranchId.Value)
    //                                       && f.Date >= fromD && f.Date <= toD
    //                                 group f by v.CompanyBranchId into g
    //                                 select new { BranchId = g.Key.Value, FuelSpend = g.Sum(x => (decimal?)x.Cost) ?? 0m })
    //                                .ToListAsync(ct);
    //        var fuelMap = fuelGrouped.ToDictionary(x => x.BranchId, x => x.FuelSpend);

    //        var maintGrouped = await _db.Set<Invoice>().AsNoTracking()
    //            .Where(i => i.CompanyBranch.CompanyId == companyId && i.CompanyBranchId != null && branchIds.Contains(i.CompanyBranchId.Value) && i.InvoiceDate >= fromD && i.InvoiceDate <= toD)
    //            .GroupBy(i => i.CompanyBranchId)
    //            .Select(g => new { BranchId = g.Key.Value, Spend = g.Sum(x => (decimal?)x.TotalAmount) ?? 0m })
    //            .ToListAsync(ct);
    //        var maintMap = maintGrouped.ToDictionary(x => x.BranchId, x => x.Spend);

    //        var finesGrouped = await _db.Set<FineAndToll>().AsNoTracking()
    //            .Where(f => f.CompanyBranch.CompanyId == companyId && f.CompanyBranchId != null && branchIds.Contains(f.CompanyBranchId.Value) && f.PaidDate != null && f.PaidDate >= fromD && f.PaidDate <= toD)
    //            .GroupBy(f => f.CompanyBranchId)
    //            .Select(g => new { BranchId = g.Key.Value, Spend = g.Sum(x => (decimal?)x.Amount) ?? 0m })
    //            .ToListAsync(ct);
    //        var finesMap = finesGrouped.ToDictionary(x => x.BranchId, x => x.Spend);

    //        // company admin lookup (for branches in scope)
    //        var adminMap = await _db.Set<CompanyAdmin>().AsNoTracking()
    //            .Where(ca => ca.CompanyId == companyId && ca.CompanyBranchId != null && branchIds.Contains(ca.CompanyBranchId.Value))
    //            .Select(ca => new { BranchId = ca.CompanyBranchId.Value, Name = ca.User.FirstName + " " + ca.User.LastName })
    //            .ToDictionaryAsync(x => x.BranchId, x => x.Name, ct);

    //        // build BranchSummaryDto list for branches in scope (either all or single branch)
    //        var branchSummaries = new List<BranchSummaryDto>();
    //        foreach (var b in branchesAll.Where(b => branchIds.Contains(b.Id)))
    //        {
    //            // Use typed records and default instances when missing
    //            driversMap.TryGetValue(b.Id, out var dg);
    //            dg ??= new BranchDriverStats(b.Id, 0, 0);

    //            vehiclesMap.TryGetValue(b.Id, out var vg);
    //            vg ??= new BranchVehicleStats(b.Id, 0, 0);

    //            assignedMap.TryGetValue(b.Id, out var assignedVehicles);
    //            ticketsMap.TryGetValue(b.Id, out var tk);
    //            tk ??= new BranchTicketStats(b.Id, 0, 0);

    //            var fuelSpend = fuelMap.TryGetValue(b.Id, out var fs) ? fs : 0m;
    //            var maintSpend = maintMap.TryGetValue(b.Id, out var ms) ? ms : 0m;
    //            var finesSpend = finesMap.TryGetValue(b.Id, out var fs2) ? fs2 : 0m;

    //            var bs = new BranchSummaryDto
    //            {
    //                BranchId = b.Id,
    //                BranchName = b.Name,
    //                ManagerName = b.ManagerName,
    //                TotalDrivers = dg.Total,
    //                ActiveDrivers = dg.Active,
    //                TotalVehicles = vg.Total,
    //                ActiveVehicles = vg.Active,
    //                AssignedVehicleCount = assignedVehicles,
    //                OpenMaintenanceTickets = tk.Open,
    //                OverdueMaintenanceTickets = tk.Overdue,
    //                VendorsCount = vendorsMap.TryGetValue(b.Id, out var vc) ? vc : 0,
    //                FuelSpend = fuelSpend,
    //                MaintenanceSpend = maintSpend,
    //                FinesSpend = finesSpend,
    //                CompanyAdminName = adminMap.TryGetValue(b.Id, out var nm) ? nm : null,
    //                TotalSpend = fuelSpend + maintSpend + finesSpend
    //            };

    //            // Performance calculation
    //            if (vg.Total > 0)
    //            {
    //                var utilizationRate = (double)assignedVehicles / vg.Total;
    //                var expensePerVehicle = vg.Total > 0 ? (double)bs.TotalSpend / vg.Total : 0;
    //                var expenseEfficiency = expensePerVehicle > 0 ? Math.Max(0, 100 - (expensePerVehicle / 10000)) : 100;
    //                bs.PerformancePercentage = Math.Min(100, (utilizationRate * 50) + (expenseEfficiency * 0.5));
    //            }
    //            else
    //            {
    //                bs.PerformancePercentage = 0;
    //            }

    //            branchSummaries.Add(bs);
    //        }

    //        dto.Branches = branchSummaries;

    //        // Calculate totals (these now reflect the scope)
    //        dto.Totals.TotalDrivers = dto.DriverCount;
    //        dto.Totals.TotalVehicles = dto.VehicleCount;
    //        dto.Totals.AssignedVehicles = assignedVehicleCount;
    //        dto.Totals.FuelSpend = branchSummaries.Sum(x => x.FuelSpend);
    //        dto.Totals.MaintenanceSpend = branchSummaries.Sum(x => x.MaintenanceSpend);
    //        dto.Totals.FinesSpend = branchSummaries.Sum(x => x.FinesSpend);
    //        dto.Totals.TotalSpend = dto.Totals.FuelSpend + dto.Totals.MaintenanceSpend + dto.Totals.FinesSpend;

    //        // Keep branches list for dropdown (all branches)
    //        // We want the DTO to include the company branches for the dropdown in the UI.
    //        dto.AllBranches = branchesAll.Select(b => new BranchListItemDto { BranchId = b.Id, BranchName = b.Name }).ToList();

    //        _cache.Set(cacheKey, dto, CacheTtl);
    //        return dto;
    //    }

    //    public async Task<BranchDetailDto> GetBranchDetailsAsync(long branchId, DashboardRequestDto req, CancellationToken ct = default)
    //    {
    //        // ensure branch exists and belongs to company (validate CompanyId from req)
    //        var branch = await _db.Set<CompanyBranch>().AsNoTracking().FirstOrDefaultAsync(b => b.Id == branchId, ct)
    //                     ?? throw new KeyNotFoundException("Branch not found");

    //        // create a minimal request for admin service calls
    //        var adminReq = new DashboardRequestDto
    //        {
    //            CompanyBranchId = branchId,
    //            DateFrom = req.DateFrom,
    //            DateTo = req.DateTo,
    //            RecentListSize = Math.Max(1, req.RecentListSize)
    //        };

    //        var detail = new BranchDetailDto
    //        {
    //            BranchId = branch.Id,
    //            BranchName = branch.Name,
    //            ManagerName = branch.ManagerName,
    //            Summary = (await GetCompanyOwnerDashboardAsync(new DashboardRequestDto
    //            {
    //                CompanyId = req.CompanyId,
    //                CompanyBranchId = branchId,
    //                DateFrom = req.DateFrom,
    //                DateTo = req.DateTo,
    //                RecentListSize = Math.Max(1, req.RecentListSize)
    //            }, ct)).Branches.FirstOrDefault(b => b.BranchId == branchId) // reuse
    //        };

    //        // Reuse AdminDashboardService to get branch-specific small lists & charts
    //        detail.RecentFuelLogs = await _adminDashboard.GetRecentFuelLogsAsync(adminReq, ct);
    //        detail.RecentMaintenanceTickets = await _adminDashboard.GetRecentMaintenanceTicketsAsync(adminReq, ct);
    //        detail.TopVehiclesByFuel = await _adminDashboard.GetTopVehiclesByFuelAsync(adminReq, 10, ct);
    //        detail.ExpensesByMonth = await _adminDashboard.GetMaintenanceByMonthAsync(adminReq, ct); // maintenance chart
    //        return detail;
    //    }

    //    public async Task<List<MonthPointDto>> GetCompanyExpensesByMonthAsync(DashboardRequestDto req, CancellationToken ct = default)
    //    {
    //        var (fromDate, toDate) = ResolveRange(req);
    //        var companyId = req.CompanyId ?? throw new ArgumentException("CompanyId required", nameof(req.CompanyId));
    //        var branchFilter = req.CompanyBranchId;
    //        var cacheKey = $"CompanyExpensesByMonth:Company:{companyId}:Branch:{branchFilter?.ToString() ?? "All"}:From:{fromDate:yyyyMMdd}:To:{toDate:yyyyMMdd}";
    //        if (_cache.TryGetValue(cacheKey, out List<MonthPointDto> cached)) return cached;

    //        // Combine fuel (cost) + maintenance (invoice total) + fines (paid) per month
    //        var fuelQ = from f in _db.Set<FuelLog>().AsNoTracking()
    //                    join v in _db.Set<Vehicle>().AsNoTracking() on f.VehicleId equals v.Id
    //                    where v.CompanyId == companyId
    //                          && f.Date >= fromDate && f.Date <= toDate
    //                          && (!branchFilter.HasValue || (v.CompanyBranchId != null && v.CompanyBranchId == branchFilter.Value))
    //                    group f by new { f.Date.Year, f.Date.Month } into g
    //                    select new { g.Key.Year, g.Key.Month, Fuel = g.Sum(x => (decimal?)x.Cost) ?? 0m };

    //        var maintQ = from i in _db.Set<Invoice>().AsNoTracking()
    //                     where i.CompanyBranch.CompanyId == companyId
    //                           && i.InvoiceDate >= fromDate && i.InvoiceDate <= toDate
    //                           && (!branchFilter.HasValue || (i.CompanyBranchId != null && i.CompanyBranchId == branchFilter.Value))
    //                     group i by new { i.InvoiceDate.Year, i.InvoiceDate.Month } into g
    //                     select new { g.Key.Year, g.Key.Month, Maint = g.Sum(x => (decimal?)x.TotalAmount) ?? 0m };

    //        var finesQ = from f in _db.Set<FineAndToll>().AsNoTracking()
    //                     where f.CompanyBranch.CompanyId == companyId && f.PaidDate != null && f.PaidDate >= fromDate && f.PaidDate <= toDate
    //                           && (!branchFilter.HasValue || (f.CompanyBranchId != null && f.CompanyBranchId == branchFilter.Value))
    //                     group f by new { f.PaidDate.Value.Year, f.PaidDate.Value.Month } into g
    //                     select new { Year = g.Key.Year, Month = g.Key.Month, Fines = g.Sum(x => (decimal?)x.Amount) ?? 0m };

    //        var fuelList = await fuelQ.ToListAsync(ct);
    //        var maintList = await maintQ.ToListAsync(ct);
    //        var finesList = await finesQ.ToListAsync(ct);

    //        var keys = fuelList.Select(x => (x.Year, x.Month))
    //                    .Union(maintList.Select(x => (x.Year, x.Month)))
    //                    .Union(finesList.Select(x => (x.Year, x.Month)))
    //                    .Distinct();

    //        var result = keys.Select(k => new MonthPointDto
    //        {
    //            Year = k.Year,
    //            Month = k.Month,
    //            Value = (fuelList.FirstOrDefault(x => x.Year == k.Year && x.Month == k.Month)?.Fuel ?? 0m)
    //                    + (maintList.FirstOrDefault(x => x.Year == k.Year && x.Month == k.Month)?.Maint ?? 0m)
    //                    + (finesList.FirstOrDefault(x => x.Year == k.Year && x.Month == k.Month)?.Fines ?? 0m),
    //            SecondaryValue = fuelList.FirstOrDefault(x => x.Year == k.Year && x.Month == k.Month)?.Fuel ?? 0m
    //        }).OrderBy(p => p.Year).ThenBy(p => p.Month).ToList();

    //        _cache.Set(cacheKey, result, CacheTtl);
    //        return result;
    //    }
    //}

    #endregion





    //public class CompanyOwnerDashboardService : ICompanyOwnerDashboardService
    //{
    //    private readonly FleetManagerDbContext _db;
    //    private readonly IMemoryCache _cache;
    //    private readonly ILogger<CompanyOwnerDashboardService> _logger;
    //    private readonly IAdminDashboardService _adminDashboard; // reuse existing granular methods
    //    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(20);

    //    public CompanyOwnerDashboardService(
    //        FleetManagerDbContext db,
    //        IMemoryCache cache,
    //        ILogger<CompanyOwnerDashboardService> logger,
    //        IAdminDashboardService adminDashboard)
    //    {
    //        _db = db ?? throw new ArgumentNullException(nameof(db));
    //        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    //        _logger = logger;
    //        _adminDashboard = adminDashboard;
    //    }

    //    private record BranchDriverStats(long BranchId, int Total, int Active);
    //    private record BranchVehicleStats(long BranchId, int Total, int Active);
    //    private record BranchTicketStats(long BranchId, int Open, int Overdue);

    //    private (DateTime fromDate, DateTime toDate) ResolveRange(DashboardRequestDto req)
    //    {
    //        var toDate = (req.DateTo ?? DateTimeOffset.UtcNow).UtcDateTime;
    //        var fromDate = (req.DateFrom ?? DateTimeOffset.UtcNow.AddMonths(-6)).UtcDateTime;
    //        return (fromDate, toDate);
    //    }


    //    public async Task<CompanyOwnerDashboardDto> GetCompanyOwnerDashboardAsync(DashboardRequestDto req, CancellationToken ct = default)
    //    {
    //        if (req.CompanyId == null) throw new ArgumentException("CompanyId required", nameof(req.CompanyId));
    //        var (fromDate, toDate) = ResolveRange(req);
    //        var companyId = req.CompanyId.Value;
    //        var cacheKey = $"CompanyOwnerDashboard:Company:{companyId}:From:{fromDate:yyyyMMdd}:To:{toDate:yyyyMMdd}";

    //        if (_cache.TryGetValue(cacheKey, out CompanyOwnerDashboardDto cached))
    //        {
    //            cached.CacheHit = true;
    //            return cached;
    //        }

    //        var dto = new CompanyOwnerDashboardDto
    //        {
    //            CompanyId = companyId,
    //            GeneratedAt = DateTimeOffset.UtcNow,
    //            TimeZoneId = req.TimeZoneId ?? "UTC",
    //            CacheHit = false,
    //            Totals = new TotalsDto()
    //        };

    //        // basic counts (branches, admins, vehicles, drivers) - ALL VEHICLES/DRIVERS UNDER COMPANY
    //        dto.BranchCount = await _db.Set<CompanyBranch>().AsNoTracking().CountAsync(b => b.CompanyId == companyId, ct);
    //        dto.AdminCount = await _db.Set<CompanyAdmin>().AsNoTracking().CountAsync(a => a.CompanyId == companyId, ct);

    //        // Get ALL vehicles and drivers under company
    //        dto.VehicleCount = await _db.Set<Vehicle>().AsNoTracking().CountAsync(v => v.CompanyBranch.CompanyId == companyId, ct);
    //        dto.DriverCount = await _db.Set<Driver>().AsNoTracking().CountAsync(d => d.CompanyId == companyId, ct);

    //        // vehicle status distribution (pie chart)
    //        var vehicleStatuses = await _db.Set<Vehicle>().AsNoTracking()
    //            .Where(v => v.CompanyId == companyId)
    //            .GroupBy(v => v.VehicleStatus)
    //            .Select(g => new { Status = g.Key, Count = g.Count() })
    //            .ToListAsync(ct);

    //        // assigned vehicle count company-wide (distinct vehicles assigned)
    //        var assignedVehicleCount = await (from dv in _db.Set<DriverVehicle>().AsNoTracking()
    //                                          join v in _db.Set<Vehicle>().AsNoTracking() on dv.VehicleId equals v.Id
    //                                          where v.CompanyId == companyId
    //                                          select dv.VehicleId)
    //                                         .Distinct()
    //                                         .CountAsync(ct);

    //        dto.VehicleStatusDistribution = new Dictionary<string, int>();
    //        foreach (var status in vehicleStatuses)
    //        {
    //            dto.VehicleStatusDistribution[status.Status.ToString()] = status.Count;
    //        }

    //        var totalVehicles = vehicleStatuses.Sum(s => s.Count);
    //        var unassignedCount = totalVehicles - assignedVehicleCount;
    //        if (unassignedCount > 0)
    //        {
    //            dto.VehicleStatusDistribution["Unassigned"] = unassignedCount;
    //        }

    //        // branches and ids
    //        var branches = await _db.Set<CompanyBranch>().AsNoTracking()
    //            .Where(b => b.CompanyId == companyId)
    //            .Select(b => new { b.Id, b.Name, b.ManagerName })
    //            .ToListAsync(ct);

    //        var branchIds = branches.Select(x => x.Id).ToList();

    //        // drivers grouped -> BranchDriverStats
    //        var driversGrouped = await _db.Set<Driver>().AsNoTracking()
    //            .Where(d => d.CompanyId == companyId && d.CompanyBranchId != null && branchIds.Contains(d.CompanyBranchId.Value))
    //            .GroupBy(d => d.CompanyBranchId)
    //            .Select(g => new BranchDriverStats(g.Key.Value, g.Count(), g.Count(d => d.IsActive)))
    //            .ToListAsync(ct);
    //        var driversMap = driversGrouped.ToDictionary(x => x.BranchId, x => x);

    //        // vehicles grouped -> BranchVehicleStats
    //        var vehiclesGrouped = await _db.Set<Vehicle>().AsNoTracking()
    //            .Where(v => v.CompanyId == companyId && v.CompanyBranchId != null && branchIds.Contains(v.CompanyBranchId.Value))
    //            .GroupBy(v => v.CompanyBranchId)
    //            .Select(g => new BranchVehicleStats(g.Key.Value, g.Count(), g.Count(v => v.VehicleStatus == VehicleStatus.Active)))
    //            .ToListAsync(ct);
    //        var vehiclesMap = vehiclesGrouped.ToDictionary(x => x.BranchId, x => x);

    //        // assigned vehicles grouped per branch
    //        var assignedGrouped = await (from dv in _db.Set<DriverVehicle>().AsNoTracking()
    //                                     join v in _db.Set<Vehicle>().AsNoTracking() on dv.VehicleId equals v.Id
    //                                     where v.CompanyId == companyId && v.CompanyBranchId != null && branchIds.Contains(v.CompanyBranchId.Value)
    //                                     group dv by v.CompanyBranchId into g
    //                                     select new { BranchId = g.Key.Value, Assigned = g.Select(x => x.VehicleId).Distinct().Count() })
    //                                    .ToListAsync(ct);
    //        var assignedMap = assignedGrouped.ToDictionary(x => x.BranchId, x => x.Assigned);

    //        // tickets grouped (range)
    //        var (fromD, toD) = ResolveRange(req);
    //        var ticketsGrouped = await _db.Set<MaintenanceTicket>().AsNoTracking()
    //            .Where(t => t.CompanyBranch.CompanyId == companyId && t.CompanyBranchId != null && branchIds.Contains(t.CompanyBranchId.Value) && t.CreatedDate >= fromD && t.CreatedDate <= toD)
    //            .GroupBy(t => t.CompanyBranchId)
    //            .Select(g => new BranchTicketStats(
    //                 g.Key.Value,
    //                 g.Count(t => t.Status != TicketStatus.Resolved && t.Status != TicketStatus.Rejected),
    //                 g.Count(t => t.Priority == MaintenancePriority.High && t.Status == TicketStatus.Pending && t.CreatedDate <= DateTime.UtcNow.AddDays(-7))
    //            ))
    //            .ToListAsync(ct);
    //        var ticketsMap = ticketsGrouped.ToDictionary(x => x.BranchId, x => x);

    //        // vendors grouped
    //        var vendorsGrouped = await _db.Set<ContactDirectory>().AsNoTracking()
    //            .Where(c => c.CompanyBranch.CompanyId == companyId && c.CompanyBranchId != null && branchIds.Contains(c.CompanyBranchId.Value))
    //            .GroupBy(c => c.CompanyBranchId)
    //            .Select(g => new { BranchId = g.Key.Value, Count = g.Count() })
    //            .ToListAsync(ct);
    //        var vendorsMap = vendorsGrouped.ToDictionary(x => x.BranchId, x => x.Count);

    //        // financials grouped (fuel, invoice, fines)
    //        var fuelGrouped = await (from f in _db.Set<FuelLog>().AsNoTracking()
    //                                 join v in _db.Set<Vehicle>().AsNoTracking() on f.VehicleId equals v.Id
    //                                 where v.CompanyId == companyId && v.CompanyBranchId != null && branchIds.Contains(v.CompanyBranchId.Value) && f.Date >= fromD && f.Date <= toD
    //                                 group f by v.CompanyBranchId into g
    //                                 select new { BranchId = g.Key.Value, FuelSpend = g.Sum(x => (decimal?)x.Cost) ?? 0m }).ToListAsync(ct);
    //        var fuelMap = fuelGrouped.ToDictionary(x => x.BranchId, x => x.FuelSpend);

    //        var maintGrouped = await _db.Set<Invoice>().AsNoTracking()
    //            .Where(i => i.CompanyBranch.CompanyId == companyId && i.CompanyBranchId != null && branchIds.Contains(i.CompanyBranchId.Value) && i.InvoiceDate >= fromD && i.InvoiceDate <= toD)
    //            .GroupBy(i => i.CompanyBranchId)
    //            .Select(g => new { BranchId = g.Key.Value, Spend = g.Sum(x => (decimal?)x.TotalAmount) ?? 0m }).ToListAsync(ct);
    //        var maintMap = maintGrouped.ToDictionary(x => x.BranchId, x => x.Spend);

    //        var finesGrouped = await _db.Set<FineAndToll>().AsNoTracking()
    //            .Where(f => f.CompanyBranch.CompanyId == companyId && f.CompanyBranchId != null && branchIds.Contains(f.CompanyBranchId.Value) && f.PaidDate != null && f.PaidDate >= fromD && f.PaidDate <= toD)
    //            .GroupBy(f => f.CompanyBranchId)
    //            .Select(g => new { BranchId = g.Key.Value, Spend = g.Sum(x => (decimal?)x.Amount) ?? 0m }).ToListAsync(ct);
    //        var finesMap = finesGrouped.ToDictionary(x => x.BranchId, x => x.Spend);

    //        // company admin lookup
    //        var adminMap = await _db.Set<CompanyAdmin>().AsNoTracking()
    //            .Where(ca => ca.CompanyId == companyId && ca.CompanyBranchId != null && branchIds.Contains(ca.CompanyBranchId.Value))
    //            .Select(ca => new { BranchId = ca.CompanyBranchId.Value, Name = ca.User.FirstName + " " + ca.User.LastName })
    //            .ToDictionaryAsync(x => x.BranchId, x => x.Name, ct);

    //        // build BranchSummaryDto list
    //        var branchSummaries = new List<BranchSummaryDto>();
    //        foreach (var b in branches)
    //        {
    //            // Use typed records and default instances when missing
    //            driversMap.TryGetValue(b.Id, out var dg);
    //            dg ??= new BranchDriverStats(b.Id, 0, 0);

    //            vehiclesMap.TryGetValue(b.Id, out var vg);
    //            vg ??= new BranchVehicleStats(b.Id, 0, 0);

    //            assignedMap.TryGetValue(b.Id, out var assignedVehicles);
    //            ticketsMap.TryGetValue(b.Id, out var tk);
    //            tk ??= new BranchTicketStats(b.Id, 0, 0);

    //            var fuelSpend = fuelMap.TryGetValue(b.Id, out var fs) ? fs : 0m;
    //            var maintSpend = maintMap.TryGetValue(b.Id, out var ms) ? ms : 0m;
    //            var finesSpend = finesMap.TryGetValue(b.Id, out var fs2) ? fs2 : 0m;

    //            var bs = new BranchSummaryDto
    //            {
    //                BranchId = b.Id,
    //                BranchName = b.Name,
    //                ManagerName = b.ManagerName,
    //                TotalDrivers = dg.Total,
    //                ActiveDrivers = dg.Active,
    //                TotalVehicles = vg.Total,
    //                ActiveVehicles = vg.Active,
    //                AssignedVehicleCount = assignedVehicles,
    //                OpenMaintenanceTickets = tk.Open,
    //                OverdueMaintenanceTickets = tk.Overdue,
    //                VendorsCount = vendorsMap.TryGetValue(b.Id, out var vc) ? vc : 0,
    //                FuelSpend = fuelSpend,
    //                MaintenanceSpend = maintSpend,
    //                FinesSpend = finesSpend,
    //                CompanyAdminName = adminMap.TryGetValue(b.Id, out var nm) ? nm : null,
    //                TotalSpend = fuelSpend + maintSpend + finesSpend
    //            };

    //            // Performance calculation
    //            if (vg.Total > 0)
    //            {
    //                var utilizationRate = (double)assignedVehicles / vg.Total;
    //                var expensePerVehicle = vg.Total > 0 ? (double)bs.TotalSpend / vg.Total : 0;
    //                var expenseEfficiency = expensePerVehicle > 0 ? Math.Max(0, 100 - (expensePerVehicle / 10000)) : 100;
    //                bs.PerformancePercentage = Math.Min(100, (utilizationRate * 50) + (expenseEfficiency * 0.5));
    //            }
    //            else
    //            {
    //                bs.PerformancePercentage = 0;
    //            }

    //            branchSummaries.Add(bs);
    //        }

    //        dto.Branches = branchSummaries;

    //        // Calculate totals
    //        dto.Totals.TotalDrivers = dto.DriverCount;
    //        dto.Totals.TotalVehicles = dto.VehicleCount;
    //        dto.Totals.AssignedVehicles = assignedVehicleCount;
    //        dto.Totals.FuelSpend = branchSummaries.Sum(x => x.FuelSpend);
    //        dto.Totals.MaintenanceSpend = branchSummaries.Sum(x => x.MaintenanceSpend);
    //        dto.Totals.FinesSpend = branchSummaries.Sum(x => x.FinesSpend);
    //        dto.Totals.TotalSpend = dto.Totals.FuelSpend + dto.Totals.MaintenanceSpend + dto.Totals.FinesSpend;

    //        _cache.Set(cacheKey, dto, CacheTtl);
    //        return dto;
    //    }

    //    //public async Task<CompanyOwnerDashboardDto> GetCompanyOwnerDashboardAsync(DashboardRequestDto req, CancellationToken ct = default)
    //    //{
    //    //    if (req.CompanyId == null) throw new ArgumentException("CompanyId required", nameof(req.CompanyId));
    //    //    var (fromDate, toDate) = ResolveRange(req);
    //    //    var companyId = req.CompanyId.Value;
    //    //    var cacheKey = $"CompanyOwnerDashboard:Company:{companyId}:From:{fromDate:yyyyMMdd}:To:{toDate:yyyyMMdd}";

    //    //    if (_cache.TryGetValue(cacheKey, out CompanyOwnerDashboardDto cached))
    //    //    {
    //    //        cached.CacheHit = true;
    //    //        return cached;
    //    //    }

    //    //    var dto = new CompanyOwnerDashboardDto
    //    //    {
    //    //        CompanyId = companyId,
    //    //        GeneratedAt = DateTimeOffset.UtcNow,
    //    //        TimeZoneId = req.TimeZoneId ?? "UTC",
    //    //        CacheHit = false
    //    //    };

    //    //    // basic counts (branches, admins, vehicles, drivers)
    //    //    dto.BranchCount = await _db.Set<CompanyBranch>().AsNoTracking().CountAsync(b => b.CompanyId == companyId, ct);
    //    //    dto.AdminCount = await _db.Set<CompanyAdmin>().AsNoTracking().CountAsync(a => a.CompanyId == companyId, ct); // adjust model
    //    //    dto.VehicleCount = await _db.Set<Vehicle>().AsNoTracking().CountAsync(v => v.CompanyId == companyId, ct);
    //    //    dto.DriverCount = await _db.Set<Driver>().AsNoTracking().CountAsync(d => d.CompanyId == companyId, ct);

    //    //    // summary per branch (same approach as earlier message) - grouped queries to minimize roundtrips
    //    //    var branches = await _db.Set<CompanyBranch>().AsNoTracking()
    //    //        .Where(b => b.CompanyId == companyId)
    //    //        .Select(b => new { b.Id, b.Name, b.ManagerName })
    //    //        .ToListAsync(ct);

    //    //    var branchIds = branches.Select(x => x.Id).ToList();

    //    //    // drivers grouped
    //    //    var driversGrouped = await _db.Set<Driver>().AsNoTracking()
    //    //        .Where(d => d.CompanyId == companyId && d.CompanyBranchId != null && branchIds.Contains(d.CompanyBranchId.Value))
    //    //        .GroupBy(d => d.CompanyBranchId)
    //    //        .Select(g => new { BranchId = g.Key.Value, Total = g.Count(), Active = g.Count(d => d.IsActive) })
    //    //        .ToListAsync(ct);

    //    //    var driversMap = driversGrouped.ToDictionary(x => x.BranchId, x => x);

    //    //    // vehicles grouped
    //    //    var vehiclesGrouped = await _db.Set<Vehicle>().AsNoTracking()
    //    //        .Where(v => v.CompanyId == companyId && v.CompanyBranchId != null && branchIds.Contains(v.CompanyBranchId.Value))
    //    //        .GroupBy(v => v.CompanyBranchId)
    //    //        .Select(g => new { BranchId = g.Key.Value, Total = g.Count(), Active = g.Count(v => v.VehicleStatus == VehicleStatus.Active) })
    //    //        .ToListAsync(ct);
    //    //    var vehiclesMap = vehiclesGrouped.ToDictionary(x => x.BranchId, x => x);

    //    //    // assigned vehicles grouped
    //    //    var assignedGrouped = await (from dv in _db.Set<DriverVehicle>().AsNoTracking()
    //    //                                 join v in _db.Set<Vehicle>().AsNoTracking() on dv.VehicleId equals v.Id
    //    //                                 where v.CompanyId == companyId && v.CompanyBranchId != null && branchIds.Contains(v.CompanyBranchId.Value)
    //    //                                 group dv by v.CompanyBranchId into g
    //    //                                 select new { BranchId = g.Key.Value, Assigned = g.Select(x => x.VehicleId).Distinct().Count() })
    //    //                                .ToListAsync(ct);
    //    //    var assignedMap = assignedGrouped.ToDictionary(x => x.BranchId, x => x.Assigned);

    //    //    // tickets grouped (range)
    //    //    var (fromD, toD) = ResolveRange(req);
    //    //    var ticketsGrouped = await _db.Set<MaintenanceTicket>().AsNoTracking()
    //    //        .Where(t => t.CompanyBranch.CompanyId == companyId && t.CompanyBranchId != null && branchIds.Contains(t.CompanyBranchId.Value) && t.CreatedDate >= fromD && t.CreatedDate <= toD)
    //    //        .GroupBy(t => t.CompanyBranchId)
    //    //        .Select(g => new {
    //    //            BranchId = g.Key.Value,
    //    //            Open = g.Count(t => t.Status != TicketStatus.Resolved && t.Status != TicketStatus.Rejected),
    //    //            Overdue = g.Count(t => t.Priority == MaintenancePriority.High && t.Status == TicketStatus.Pending && t.CreatedDate <= DateTime.UtcNow.AddDays(-7))
    //    //        }).ToListAsync(ct);
    //    //    var ticketsMap = ticketsGrouped.ToDictionary(x => x.BranchId, x => x);

    //    //    // vendors grouped
    //    //    var vendorsGrouped = await _db.Set<ContactDirectory>().AsNoTracking()
    //    //        .Where(c => c.CompanyBranch.CompanyId == companyId && c.CompanyBranchId != null && branchIds.Contains(c.CompanyBranchId.Value))
    //    //        .GroupBy(c => c.CompanyBranchId)
    //    //        .Select(g => new { BranchId = g.Key.Value, Count = g.Count() })
    //    //        .ToListAsync(ct);
    //    //    var vendorsMap = vendorsGrouped.ToDictionary(x => x.BranchId, x => x.Count);

    //    //    // financials grouped (fuel, invoice, fines)
    //    //    var fuelGrouped = await (from f in _db.Set<FuelLog>().AsNoTracking()
    //    //                             join v in _db.Set<Vehicle>().AsNoTracking() on f.VehicleId equals v.Id
    //    //                             where v.CompanyId == companyId && v.CompanyBranchId != null && branchIds.Contains(v.CompanyBranchId.Value) && f.Date >= fromD && f.Date <= toD
    //    //                             group f by v.CompanyBranchId into g
    //    //                             select new { BranchId = g.Key.Value, FuelSpend = g.Sum(x => (decimal?)x.Cost) ?? 0m }).ToListAsync(ct);
    //    //    var fuelMap = fuelGrouped.ToDictionary(x => x.BranchId, x => x.FuelSpend);

    //    //    var maintGrouped = await _db.Set<Invoice>().AsNoTracking()
    //    //        .Where(i => i.CompanyBranch.CompanyId == companyId && i.CompanyBranchId != null && branchIds.Contains(i.CompanyBranchId.Value) && i.InvoiceDate >= fromD && i.InvoiceDate <= toD)
    //    //        .GroupBy(i => i.CompanyBranchId)
    //    //        .Select(g => new { BranchId = g.Key.Value, Spend = g.Sum(x => (decimal?)x.TotalAmount) ?? 0m }).ToListAsync(ct);
    //    //    var maintMap = maintGrouped.ToDictionary(x => x.BranchId, x => x.Spend);

    //    //    var finesGrouped = await _db.Set<FineAndToll>().AsNoTracking()
    //    //        .Where(f => f.CompanyBranch.CompanyId == companyId && f.CompanyBranchId != null && branchIds.Contains(f.CompanyBranchId.Value) && f.PaidDate != null && f.PaidDate >= fromD && f.PaidDate <= toD)
    //    //        .GroupBy(f => f.CompanyBranchId)
    //    //        .Select(g => new { BranchId = g.Key.Value, Spend = g.Sum(x => (decimal?)x.Amount) ?? 0m }).ToListAsync(ct);
    //    //    var finesMap = finesGrouped.ToDictionary(x => x.BranchId, x => x.Spend);

    //    //    // company admin lookup (if you have CompanyAdmin table)
    //    //    var adminMap = await _db.Set<CompanyAdmin>().AsNoTracking()
    //    //        .Where(ca => ca.CompanyId == companyId && ca.CompanyBranchId != null && branchIds.Contains(ca.CompanyBranchId.Value))
    //    //        .Select(ca => new { BranchId = ca.CompanyBranchId.Value, Name = ca.User.FirstName + " " + ca.User.LastName })
    //    //        .ToDictionaryAsync(x => x.BranchId, x => x.Name, ct);

    //    //    // build BranchSummaryDto list
    //    //    var branchSummaries = new List<BranchSummaryDto>();
    //    //    foreach (var b in branches)
    //    //    {
    //    //        var bs = new BranchSummaryDto
    //    //        {
    //    //            BranchId = b.Id,
    //    //            BranchName = b.Name,
    //    //            ManagerName = b.ManagerName,
    //    //            TotalDrivers = driversMap.TryGetValue(b.Id, out var dg) ? dg.Total : 0,
    //    //            ActiveDrivers = driversMap.TryGetValue(b.Id, out dg) ? dg.Active : 0,
    //    //            TotalVehicles = vehiclesMap.TryGetValue(b.Id, out var vg) ? vg.Total : 0,
    //    //            ActiveVehicles = vehiclesMap.TryGetValue(b.Id, out vg) ? vg.Active : 0,
    //    //            AssignedVehicleCount = assignedMap.TryGetValue(b.Id, out var av) ? av : 0,
    //    //            OpenMaintenanceTickets = ticketsMap.TryGetValue(b.Id, out var tk) ? tk.Open : 0,
    //    //            OverdueMaintenanceTickets = ticketsMap.TryGetValue(b.Id, out tk) ? tk.Overdue : 0,
    //    //            VendorsCount = vendorsMap.TryGetValue(b.Id, out var vc) ? vc : 0,
    //    //            FuelSpend = fuelMap.TryGetValue(b.Id, out var fs) ? fs : 0m,
    //    //            MaintenanceSpend = maintMap.TryGetValue(b.Id, out var ms) ? ms : 0m,
    //    //            FinesSpend = finesMap.TryGetValue(b.Id, out var fs2) ? fs2 : 0m,
    //    //            CompanyAdminName = adminMap.TryGetValue(b.Id, out var nm) ? nm : null
    //    //        };
    //    //        bs.TotalSpend = bs.FuelSpend + bs.MaintenanceSpend + bs.FinesSpend;
    //    //        branchSummaries.Add(bs);
    //    //    }

    //    //    dto.Branches = branchSummaries;
    //    //    dto.Totals.TotalDrivers = branchSummaries.Sum(x => x.TotalDrivers);
    //    //    dto.Totals.TotalVehicles = branchSummaries.Sum(x => x.TotalVehicles);
    //    //    dto.Totals.AssignedVehicles = branchSummaries.Sum(x => x.AssignedVehicleCount);
    //    //    dto.Totals.FuelSpend = branchSummaries.Sum(x => x.FuelSpend);
    //    //    dto.Totals.MaintenanceSpend = branchSummaries.Sum(x => x.MaintenanceSpend);
    //    //    dto.Totals.FinesSpend = branchSummaries.Sum(x => x.FinesSpend);
    //    //    dto.Totals.TotalSpend = dto.Totals.FuelSpend + dto.Totals.MaintenanceSpend + dto.Totals.FinesSpend;

    //    //    _cache.Set(cacheKey, dto, CacheTtl);
    //    //    return dto;
    //    //}

    //    public async Task<BranchDetailDto> GetBranchDetailsAsync(long branchId, DashboardRequestDto req, CancellationToken ct = default)
    //    {
    //        // ensure branch exists and belongs to company (validate CompanyId from req)
    //        var branch = await _db.Set<CompanyBranch>().AsNoTracking().FirstOrDefaultAsync(b => b.Id == branchId, ct)
    //                     ?? throw new KeyNotFoundException("Branch not found");

    //        // create a minimal request for admin service calls
    //        var adminReq = new DashboardRequestDto
    //        {
    //            CompanyBranchId = branchId,
    //            DateFrom = req.DateFrom,
    //            DateTo = req.DateTo,
    //            RecentListSize = Math.Max(1, req.RecentListSize)
    //        };

    //        var detail = new BranchDetailDto
    //        {
    //            BranchId = branch.Id,
    //            BranchName = branch.Name,
    //            ManagerName = branch.ManagerName,
    //            Summary = (await GetCompanyOwnerDashboardAsync(req, ct)).Branches.FirstOrDefault(b => b.BranchId == branchId) // reuse
    //        };

    //        // Reuse AdminDashboardService to get branch-specific small lists & charts
    //        detail.RecentFuelLogs = await _adminDashboard.GetRecentFuelLogsAsync(adminReq, ct);
    //        detail.RecentMaintenanceTickets = await _adminDashboard.GetRecentMaintenanceTicketsAsync(adminReq, ct);
    //        detail.TopVehiclesByFuel = await _adminDashboard.GetTopVehiclesByFuelAsync(adminReq, 10, ct);
    //        detail.ExpensesByMonth = await _adminDashboard.GetMaintenanceByMonthAsync(adminReq, ct); // maintenance chart
    //                                                                                                 // You can combine fuel+maintenance into a custom company-level expenses endpoint if you prefer
    //        return detail;
    //    }

    //    public async Task<List<MonthPointDto>> GetCompanyExpensesByMonthAsync(DashboardRequestDto req, CancellationToken ct = default)
    //    {
    //        var (fromDate, toDate) = ResolveRange(req);
    //        var companyId = req.CompanyId ?? throw new ArgumentException("CompanyId required", nameof(req.CompanyId));
    //        var cacheKey = $"CompanyExpensesByMonth:Company:{companyId}:From:{fromDate:yyyyMMdd}:To:{toDate:yyyyMMdd}";
    //        if (_cache.TryGetValue(cacheKey, out List<MonthPointDto> cached)) return cached;

    //        // Combine fuel (cost) + maintenance (invoice total) + fines (paid) per month
    //        var fuelQ = from f in _db.Set<FuelLog>().AsNoTracking()
    //                    join v in _db.Set<Vehicle>().AsNoTracking() on f.VehicleId equals v.Id
    //                    where v.CompanyId == companyId && f.Date >= fromDate && f.Date <= toDate
    //                    group f by new { f.Date.Year, f.Date.Month } into g
    //                    select new { g.Key.Year, g.Key.Month, Fuel = g.Sum(x => (decimal?)x.Cost) ?? 0m };

    //        var maintQ = from i in _db.Set<Invoice>().AsNoTracking()
    //                     where i.CompanyBranch.CompanyId == companyId && i.InvoiceDate >= fromDate && i.InvoiceDate <= toDate
    //                     group i by new { i.InvoiceDate.Year, i.InvoiceDate.Month } into g
    //                     select new { g.Key.Year, g.Key.Month, Maint = g.Sum(x => (decimal?)x.TotalAmount) ?? 0m };

    //        var finesQ = from f in _db.Set<FineAndToll>().AsNoTracking()
    //                     where f.CompanyBranch.CompanyId == companyId && f.PaidDate != null && f.PaidDate >= fromDate && f.PaidDate <= toDate
    //                     group f by new { f.PaidDate.Value.Year, f.PaidDate.Value.Month } into g
    //                     select new { Year = g.Key.Year, Month = g.Key.Month, Fines = g.Sum(x => (decimal?)x.Amount) ?? 0m };

    //        // Use ToListAsync then merge in memory (simple and clear)
    //        var fuelList = await fuelQ.ToListAsync(ct);
    //        var maintList = await maintQ.ToListAsync(ct);
    //        var finesList = await finesQ.ToListAsync(ct);

    //        var keys = fuelList.Select(x => (x.Year, x.Month)).Union(maintList.Select(x => (x.Year, x.Month))).Union(finesList.Select(x => (x.Year, x.Month))).Distinct();

    //        var result = keys.Select(k => new MonthPointDto
    //        {
    //            Year = k.Year,
    //            Month = k.Month,
    //            Value = (fuelList.FirstOrDefault(x => x.Year == k.Year && x.Month == k.Month)?.Fuel ?? 0m)
    //                    + (maintList.FirstOrDefault(x => x.Year == k.Year && x.Month == k.Month)?.Maint ?? 0m)
    //                    + (finesList.FirstOrDefault(x => x.Year == k.Year && x.Month == k.Month)?.Fines ?? 0m),
    //            SecondaryValue = fuelList.FirstOrDefault(x => x.Year == k.Year && x.Month == k.Month)?.Fuel ?? 0m
    //        }).OrderBy(p => p.Year).ThenBy(p => p.Month).ToList();

    //        _cache.Set(cacheKey, result, CacheTtl);
    //        return result;
    //    }
    //}

}
