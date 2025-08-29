using FleetManager.Business.Database.Entities;
using FleetManager.Business.Database.Entities.MaintenanceTicket;
using FleetManager.Business.DataObjects.AdminDashboardDto;
using FleetManager.Business.Enums;
using FleetManager.Business.Interfaces.AdminDashboardModule;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Implementations.AdminDashboardService
{
    public class AdminDashboardService : IAdminDashboardService
    {
        private readonly FleetManagerDbContext _db;
        private readonly IMemoryCache _cache;
        private readonly ILogger<AdminDashboardService> _logger;
        private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(30);
        
        public AdminDashboardService(FleetManagerDbContext db, IMemoryCache cache, ILogger<AdminDashboardService> logger)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger;
        }

        // Helper: date range defaults (renamed to avoid 'from' keyword collision)
        private (DateTime fromDate, DateTime toDate) ResolveRange(DashboardRequestDto req)
        {
            var toDate = (req.DateTo ?? DateTimeOffset.UtcNow).UtcDateTime;
            var fromDate = (req.DateFrom ?? DateTimeOffset.UtcNow.AddMonths(-6)).UtcDateTime;
            return (fromDate, toDate);
        }

        public async Task<DashboardDto> GetAdminDashboardAsync(DashboardRequestDto req, CancellationToken ct = default)
        {
            var (fromDate, toDate) = ResolveRange(req);
            var branchId = req.CompanyBranchId;
            var recentSize = Math.Max(1, req.RecentListSize);

            var cacheKey = $"AdminDashboard:Branch:{branchId ?? 0}:From:{fromDate:yyyyMMdd}:To:{toDate:yyyyMMdd}:Size:{recentSize}";
            if (_cache.TryGetValue(cacheKey, out DashboardDto cached))
            {
                cached.CacheHit = true;
                return cached;
            }

            var dto = new DashboardDto
            {
                GeneratedAt = DateTimeOffset.UtcNow,
                TimeZoneId = req.TimeZoneId ?? "UTC",
                CacheHit = false
            };

            // --- Counts ---
            var driversQ = _db.Set<Driver>().AsNoTracking().Where(d => d.CompanyBranchId == branchId);
            dto.Counts.TotalDrivers = await driversQ.CountAsync(ct);
            dto.Counts.ActiveDrivers = await driversQ.CountAsync(d => d.IsActive, ct);

            var vehicleQ = _db.Set<Vehicle>().AsNoTracking().Where(v => v.CompanyBranchId == branchId);
            dto.Counts.TotalVehicles = await vehicleQ.CountAsync(ct);
            dto.Counts.ActiveVehicles = await vehicleQ.CountAsync(v => v.IsActive, ct);

            // Assigned vehicles (distinct VehicleId from DriverVehicle where vehicle belongs to branch)
            var assignedQ = from dv in _db.Set<DriverVehicle>().AsNoTracking()
                            join v in _db.Set<Vehicle>().AsNoTracking() on dv.VehicleId equals v.Id
                            where v.CompanyBranchId == branchId
                            select dv.VehicleId;
            dto.Counts.AssignedVehicleCount = await assignedQ.Distinct().CountAsync(ct);

            // Open / overdue tickets
            dto.Counts.OpenMaintenanceTickets = await _db.Set<MaintenanceTicket>()
                .AsNoTracking()
                .CountAsync(t => t.CompanyBranchId == branchId && t.Status != TicketStatus.Resolved && t.Status != TicketStatus.Rejected, ct);

            dto.Counts.OverdueMaintenanceTickets = await _db.Set<MaintenanceTicket>()
                .AsNoTracking()
                .CountAsync(t => t.CompanyBranchId == branchId && t.Priority == MaintenancePriority.High && t.Status == TicketStatus.Pending && t.CreatedDate <= DateTime.UtcNow.AddDays(-7), ct);

            dto.Counts.OpenFines = await _db.Set<FineAndToll>()
                .AsNoTracking()
                .CountAsync(f => f.CompanyBranchId == branchId && f.Status != FineTollStatus.Paid, ct);

            // ContactDirectory = vendors list for Admin
            dto.Counts.VendorsCount = await _db.Set<ContactDirectory>()
                .AsNoTracking()
                .CountAsync(c => c.CompanyBranchId == branchId, ct);

            // --- Money aggregates ---
            // Fuel spend: join FuelLog -> Vehicle to filter company branch
            var fuelJoin = from f in _db.Set<FuelLog>().AsNoTracking()
                           join v in _db.Set<Vehicle>().AsNoTracking() on f.VehicleId equals v.Id
                           where v.CompanyBranchId == branchId && f.Date >= fromDate && f.Date <= toDate
                           select f;

            dto.Money.FuelSpend = await fuelJoin.SumAsync(f => (decimal?)f.Cost, ct) ?? 0m;

            // Maintenance spend: use Invoice.TotalAmount (invoice.CompanyBranchId expected)
            var maintenanceInvQ = _db.Set<Invoice>().AsNoTracking()
                .Where(i => i.CompanyBranchId == branchId && i.InvoiceDate >= fromDate && i.InvoiceDate <= toDate);

            dto.Money.MaintenanceSpend = await maintenanceInvQ.SumAsync(i => (decimal?)i.TotalAmount, ct) ?? 0m;

            // Fines spend (paid)
            dto.Money.FinesSpend = await _db.Set<FineAndToll>()
                .AsNoTracking()
                .Where(f => f.CompanyBranchId == branchId && f.PaidDate != null && f.PaidDate >= fromDate && f.PaidDate <= toDate)
                .SumAsync(f => (decimal?)f.Amount, ct) ?? 0m;

            // TotalSpend (note: vendor-specific spend requires linking invoices/tickets to ContactDirectory)
            dto.Money.TotalSpend = dto.Money.FuelSpend + dto.Money.MaintenanceSpend + dto.Money.FinesSpend;

            // --- Quick previews (small lists) ---
            dto.RecentMaintenanceTickets = await GetRecentMaintenanceTicketsAsync(req, ct);
            dto.RecentFuelLogs = await GetRecentFuelLogsAsync(req, ct);
            dto.RecentContacts = await GetRecentContactsAsync(req, ct);

            // --- small previews for charts (first 4 months) ---
            var fuelByMonth = await GetFuelByMonthAsync(req, ct);
            dto.FuelByMonthPreview = fuelByMonth.OrderByDescending(m => new DateTime(m.Year, m.Month, 1)).Take(4).OrderBy(m => new DateTime(m.Year, m.Month, 1)).ToList();

            var maintByMonth = await GetMaintenanceByMonthAsync(req, ct);
            dto.MaintenanceByMonthPreview = maintByMonth.OrderByDescending(m => new DateTime(m.Year, m.Month, 1)).Take(4).OrderBy(m => new DateTime(m.Year, m.Month, 1)).ToList();

            // --- Utilization & averages ---
            var (fromD, toD) = (fromDate, toDate);
            var periodDays = (toD.Date - fromD.Date).Days + 1;

            var assignments = await _db.Set<DriverVehicle>().AsNoTracking()
                .Join(_db.Set<Vehicle>().AsNoTracking(),
                      dv => dv.VehicleId,
                      v => v.Id,
                      (dv, v) => new { dv.VehicleId, dv.StartDate, dv.EndDate, v.CompanyBranchId })
                .Where(x => x.CompanyBranchId == req.CompanyBranchId && ((x.StartDate == null && x.EndDate == null) || (x.EndDate >= fromD && x.StartDate <= toD)))
                .ToListAsync(ct);

            var assignedDaysByVehicle = assignments
                .GroupBy(a => a.VehicleId)
                .Select(g =>
                {
                    int total = g.Sum(x =>
                    {
                        var s = x.StartDate ?? DateTime.MinValue;
                        var e = x.EndDate ?? DateTime.MaxValue;
                        var s2 = s < fromD ? fromD : s;
                        var e2 = e > toD ? toD : e;
                        if (e2 < s2) return 0;
                        return (int)(e2.Date - s2.Date).TotalDays + 1;
                    });
                    return new { VehicleId = g.Key, AssignedDays = total };
                })
                .ToList();

            // Fleet utilization rate (average assigned percent)
            if (assignedDaysByVehicle.Any())
            {
                dto.Money.UtilizationRatePercent = Math.Round((decimal)assignedDaysByVehicle.Average(x => (100.0m * x.AssignedDays) / periodDays), 2);
            }
            else dto.Money.UtilizationRatePercent = 0m;

            // Avg fuel per km & cost per km (odometer-based)
            var fuelWithOdo = await fuelJoin.Where(f => f.Odometer != null)
                .GroupBy(f => f.VehicleId)
                .Select(g => new
                {
                    VehicleId = g.Key,
                    TotalVolume = g.Sum(x => x.Volume),
                    TotalCost = g.Sum(x => x.Cost),
                    MinOdo = g.Min(x => x.Odometer),
                    MaxOdo = g.Max(x => x.Odometer)
                })
                .ToListAsync(ct);

            decimal totVol = 0m, totCost = 0m;
            long totDist = 0;
            foreach (var r in fuelWithOdo)
            {
                totVol += r.TotalVolume;
                totCost += r.TotalCost;
                if (r.MinOdo.HasValue && r.MaxOdo.HasValue && r.MaxOdo.Value >= r.MinOdo.Value)
                    totDist += (long)(r.MaxOdo.Value - r.MinOdo.Value);
            }

            dto.Money.AvgFuelPerKm = totDist > 0 ? Math.Round(totVol / totDist, 4) : 0m;
            dto.Money.CostPerKm = totDist > 0 ? Math.Round(totCost / totDist, 4) : 0m;

            // Avg time to resolve maintenance
            var resolvedList = await _db.Set<MaintenanceTicket>().AsNoTracking()
                .Where(t => t.CompanyBranchId == branchId && t.ResolvedAt != null && t.ResolvedAt >= fromDate && t.ResolvedAt <= toDate)
                .Select(t => new { t.CreatedDate, t.ResolvedAt })
                .ToListAsync(ct);

            if (resolvedList.Any())
            {
                dto.Money.AvgTimeToResolveMaintenanceHours = Math.Round(resolvedList.Average(x => (x.ResolvedAt.Value - x.CreatedDate).TotalHours), 2);
            }
            else dto.Money.AvgTimeToResolveMaintenanceHours = 0;

            // Cache and return
            _cache.Set(cacheKey, dto, CacheTtl);
            return dto;
        }

        // ----------------- Chart & small endpoints (cached individually) -----------------

        public async Task<List<MonthPointDto>> GetFuelByMonthAsync(DashboardRequestDto req, CancellationToken ct = default)
        {
            var (fromDate, toDate) = ResolveRange(req);
            var branchId = req.CompanyBranchId;
            var cacheKey = $"FuelByMonth:Branch:{branchId ?? 0}:From:{fromDate:yyyyMMdd}:To:{toDate:yyyyMMdd}";
            if (_cache.TryGetValue(cacheKey, out List<MonthPointDto> cached)) return cached;

            var q = from f in _db.Set<FuelLog>().AsNoTracking()
                    join v in _db.Set<Vehicle>().AsNoTracking() on f.VehicleId equals v.Id
                    where v.CompanyBranchId == branchId && f.Date >= fromDate && f.Date <= toDate
                    group f by new { f.Date.Year, f.Date.Month } into g
                    select new MonthPointDto
                    {
                        Year = g.Key.Year,
                        Month = g.Key.Month,
                        Value = g.Sum(x => x.Cost),
                        SecondaryValue = g.Sum(x => x.Volume)
                    };

            var list = await q.OrderBy(p => p.Year).ThenBy(p => p.Month).ToListAsync(ct);
            _cache.Set(cacheKey, list, CacheTtl);
            return list;
        }

        public async Task<List<MonthPointDto>> GetMaintenanceByMonthAsync(DashboardRequestDto req, CancellationToken ct = default)
        {
            var (fromDate, toDate) = ResolveRange(req);
            var branchId = req.CompanyBranchId;
            var cacheKey = $"MaintenanceByMonth:Branch:{branchId ?? 0}:From:{fromDate:yyyyMMdd}:To:{toDate:yyyyMMdd}";
            if (_cache.TryGetValue(cacheKey, out List<MonthPointDto> cached)) return cached;

            var q = from i in _db.Set<Invoice>().AsNoTracking()
                    where i.CompanyBranchId == branchId && i.InvoiceDate >= fromDate && i.InvoiceDate <= toDate
                    group i by new { i.InvoiceDate.Year, i.InvoiceDate.Month } into g
                    select new MonthPointDto
                    {
                        Year = g.Key.Year,
                        Month = g.Key.Month,
                        Value = g.Sum(x => x.TotalAmount)
                    };

            var list = await q.OrderBy(p => p.Year).ThenBy(p => p.Month).ToListAsync(ct);
            _cache.Set(cacheKey, list, CacheTtl);
            return list;
        }

        public async Task<List<KeyValueDto>> GetTicketsByStatusAsync(DashboardRequestDto req, CancellationToken ct = default)
        {
            var (fromDate, toDate) = ResolveRange(req);
            var branchId = req.CompanyBranchId;
            var cacheKey = $"TicketsByStatus:Branch:{branchId ?? 0}:From:{fromDate:yyyyMMdd}:To:{toDate:yyyyMMdd}";
            if (_cache.TryGetValue(cacheKey, out List<KeyValueDto> cached)) return cached;

            var q = from t in _db.Set<MaintenanceTicket>().AsNoTracking()
                    where t.CompanyBranchId == branchId && t.CreatedDate >= fromDate && t.CreatedDate <= toDate
                    group t by t.Status.ToString() into g
                    select new KeyValueDto { Key = g.Key, Value = g.LongCount() };

            var list = await q.ToListAsync(ct);
            _cache.Set(cacheKey, list, CacheTtl);
            return list;
        }

        public async Task<List<TopVehicleDto>> GetTopVehiclesByFuelAsync(DashboardRequestDto req, int top = 10, CancellationToken ct = default)
        {
            var (fromDate, toDate) = ResolveRange(req);
            var branchId = req.CompanyBranchId;
            var cacheKey = $"TopVehiclesByFuel:Branch:{branchId ?? 0}:From:{fromDate:yyyyMMdd}:To:{toDate:yyyyMMdd}:Top:{top}";
            if (_cache.TryGetValue(cacheKey, out List<TopVehicleDto> cached)) return cached;

            var q = from f in _db.Set<FuelLog>().AsNoTracking()
                    join v in _db.Set<Vehicle>().AsNoTracking() on f.VehicleId equals v.Id
                    where v.CompanyBranchId == branchId && f.Date >= fromDate && f.Date <= toDate
                    group new { f, v } by new { f.VehicleId, v.PlateNo } into g
                    select new TopVehicleDto
                    {
                        VehicleId = g.Key.VehicleId ?? 0,
                        PlateNo = g.Key.PlateNo,
                        TotalVolume = g.Sum(x => x.f.Volume),
                        TotalCost = g.Sum(x => x.f.Cost),
                    };

            var list = await q.OrderByDescending(x => x.TotalVolume).Take(top).ToListAsync(ct);
            _cache.Set(cacheKey, list, CacheTtl);
            return list;
        }

        // Maintenance cost breakdown by part category using InvoiceItem
        public async Task<List<PartCategorySpendDto>> GetMaintenanceCostByPartCategoryAsync(DashboardRequestDto req, int top = 10, CancellationToken ct = default)
        {
            var (fromDate, toDate) = ResolveRange(req);
            var branchId = req.CompanyBranchId;
            var cacheKey = $"MaintenanceCostByPartCategory:Branch:{branchId ?? 0}:From:{fromDate:yyyyMMdd}:To:{toDate:yyyyMMdd}:Top:{top}";
            if (_cache.TryGetValue(cacheKey, out List<PartCategorySpendDto> cached)) return cached;

            // Join InvoiceItem -> Invoice to filter by company branch and date
            var q = from ii in _db.Set<InvoiceItem>().AsNoTracking()
                    join inv in _db.Set<Invoice>().AsNoTracking() on ii.InvoiceId equals inv.Id
                    where inv.CompanyBranchId == branchId && inv.InvoiceDate >= fromDate && inv.InvoiceDate <= toDate
                    group ii by ii.VehiclePartCategoryId into g
                    select new PartCategorySpendDto
                    {
                        VehiclePartCategoryId = g.Key,
                        Spend = g.Sum(x => x.UnitPrice * x.Quantity)
                    };

            var list = await q.OrderByDescending(x => x.Spend).Take(top).ToListAsync(ct);
            _cache.Set(cacheKey, list, CacheTtl);
            return list;
        }

        public async Task<List<RecentTicketDto>> GetRecentMaintenanceTicketsAsync(DashboardRequestDto req, CancellationToken ct = default)
        {
            var (fromDate, toDate) = ResolveRange(req);
            var branchId = req.CompanyBranchId;
            var recentSize = Math.Max(1, req.RecentListSize);
            var cacheKey = $"RecentTickets:Branch:{branchId ?? 0}:From:{fromDate:yyyyMMdd}:To:{toDate:yyyyMMdd}:Size:{recentSize}";
            if (_cache.TryGetValue(cacheKey, out List<RecentTicketDto> cached)) return cached;

            var q = _db.Set<MaintenanceTicket>().AsNoTracking()
                      .Where(t => t.CompanyBranchId == branchId && t.CreatedDate >= fromDate && t.CreatedDate <= toDate)
                      .OrderByDescending(t => t.CreatedDate)
                      .Take(recentSize)
                      .Select(t => new RecentTicketDto
                      {
                          TicketId = t.Id,
                          VehicleId = t.VehicleId,
                          VehiclePlateNo = t.Vehicle != null ? t.Vehicle.PlateNo : "",
                          DriverId = t.DriverId,
                          DriverName = t.Driver != null && t.Driver.User != null ? (t.Driver.User.FirstName + " " + t.Driver.User.LastName).Trim() : "",
                          Status = t.Status.ToString(),
                          CreatedDate = t.CreatedDate,
                          ResolvedAt = t.ResolvedAt,
                          InvoiceAmount = t.Invoice != null ? (decimal?)t.Invoice.TotalAmount : null,
                          Subject = t.Subject
                      });

            var list = await q.ToListAsync(ct);
            _cache.Set(cacheKey, list, CacheTtl);
            return list;
        }

        public async Task<List<RecentFuelDto>> GetRecentFuelLogsAsync(DashboardRequestDto req, CancellationToken ct = default)
        {
            var (fromDate, toDate) = ResolveRange(req);
            var branchId = req.CompanyBranchId;
            var recentSize = Math.Max(1, req.RecentListSize);
            var cacheKey = $"RecentFuel:Branch:{branchId ?? 0}:From:{fromDate:yyyyMMdd}:To:{toDate:yyyyMMdd}:Size:{recentSize}";
            if (_cache.TryGetValue(cacheKey, out List<RecentFuelDto> cached)) return cached;

            var q = from f in _db.Set<FuelLog>().AsNoTracking()
                    join v in _db.Set<Vehicle>().AsNoTracking() on f.VehicleId equals v.Id
                    where v.CompanyBranchId == branchId && f.Date >= fromDate && f.Date <= toDate
                    orderby f.Date descending
                    select new RecentFuelDto
                    {
                        FuelLogId = f.Id,
                        VehicleId = f.VehicleId,
                        VehiclePlateNo = v.PlateNo,
                        DriverId = f.DriverId,
                        Volume = f.Volume,
                        Cost = f.Cost,
                        Odometer = f.Odometer,
                        Date = f.Date
                    };

            var list = await q.Take(recentSize).ToListAsync(ct);
            _cache.Set(cacheKey, list, CacheTtl);
            return list;
        }

        public async Task<List<ContactDto>> GetRecentContactsAsync(DashboardRequestDto req, CancellationToken ct = default)
        {
            var branchId = req.CompanyBranchId;
            var recentSize = Math.Max(1, req.RecentListSize);
            var cacheKey = $"RecentContacts:Branch:{branchId ?? 0}:Size:{recentSize}";
            if (_cache.TryGetValue(cacheKey, out List<ContactDto> cached)) return cached;

            var q = _db.Set<ContactDirectory>().AsNoTracking()
                      .Where(c => c.CompanyBranchId == branchId)
                      .OrderByDescending(c => c.CreatedDate)
                      .Take(recentSize)
                      .Select(c => new ContactDto
                      {
                          Id = c.Id,
                          ContactName = c.ContactName,
                          PhoneNumber = c.PhoneNumber,
                          Email = c.Email,
                          Services = c.Services
                      });

            var list = await q.ToListAsync(ct);
            _cache.Set(cacheKey, list, CacheTtl);
            return list;
        }
    }
}

