using FleetManager.Business.Database.IdentityModels;
using FleetManager.Business.DataObjects.DashboardDriverDto;
using FleetManager.Business.Enums;
using FleetManager.Business.Interfaces.DriverDashboardModule;
using FleetManager.Business.UtilityModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Implementations.DriverDashboardModule
{
    public class DriverDashboardService : IDriverDashboardService
    {
        private readonly FleetManagerDbContext _context; // <- adapt the DbContext name if different
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly UserManager<ApplicationUser> _userManager;

        public DriverDashboardService(
            FleetManagerDbContext context,
            IHttpContextAccessor httpContextAccessor,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _userManager = userManager;
        }

        public async Task<MessageResponse<DriverDashboardDto>> GetDriverDashboardAsync()
        {
            var user = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext?.User);
            if (user == null) return new MessageResponse<DriverDashboardDto> { Success = false, Message = "Invalid user context." };

            // find driver record
            var driver = await _context.Drivers.AsNoTracking()
                .Include(d => d.Violations)
                .Include(d => d.DriverDocuments)
                .FirstOrDefaultAsync(d => d.UserId == user.Id);

            if (driver == null) return new MessageResponse<DriverDashboardDto> { Success = false, Message = "Driver profile not found." };

            var dashboard = new DriverDashboardDto
            {
                DriverName = $"{user.FirstName}".Trim()
            };

            // --- Assigned Vehicle (most recent active assignment or latest) ---
            var now = DateTime.UtcNow;
            var driverVehicle = await _context.DriverVehicles
                .AsNoTracking()
                .Include(dv => dv.Driver)
                .Include(dv => dv.Vehicle)
                    .ThenInclude(v => v.VehicleMake)
                .Include(dv => dv.Vehicle)
                    .ThenInclude(v => v.VehicleModel)
                .Where(dv => dv.DriverId == driver.Id)
                .OrderByDescending(dv => dv.StartDate)
                .FirstOrDefaultAsync(dv => dv.EndDate == null || dv.EndDate > now)
                ?? await _context.DriverVehicles
                .AsNoTracking()
                        .Include(dv => dv.Vehicle).ThenInclude(v => v.VehicleMake)
                        .Include(dv => dv.Vehicle).ThenInclude(v => v.VehicleModel)
                        .Where(dv => dv.DriverId == driver.Id)
                        .OrderByDescending(dv => dv.StartDate)
                        .FirstOrDefaultAsync();

            if (driverVehicle?.Vehicle != null)
            {
                var v = driverVehicle.Vehicle;
                var d = driverVehicle.Driver;
                dashboard.AssignedVehicle = new AssignedVehicleDto
                {
                    VehicleId = v.Id,
                    //MakeModel = $"{v.VehicleMake?.Name ?? string.Empty} {v.VehicleModel?.Name ?? string.Empty}".Trim(),
                    MakeModel = v.CustomMakeName != null ? (v.CustomMakeName + " " + v.CustomModelName).Trim() 
                    : (v.VehicleMake != null ? v.VehicleMake.Name : "Unknown") + " " + (v.VehicleModel != null ? v.VehicleModel.Name : ""),

                    FleetId = d.LicenseNumber,
                    PlateNo = v.PlateNo,
                    Mileage = v.Mileage,
                    FuelLevelPercent = 0, // you don't have a direct fuel level on Vehicle - set from latest FuelLog if available
                    EngineHealth = "Unknown",
                    TireCondition = "Unknown"
                };

                // set fuel level from latest FuelLog for this vehicle if present (approximation)
                var latestFuel = await _context.FuelLogs
                    .AsNoTracking()
                    .Where(f => f.VehicleId == v.Id)
                    .OrderByDescending(f => f.Date)
                    .FirstOrDefaultAsync();

                if (latestFuel != null && latestFuel.Odometer.HasValue && v.Mileage.HasValue)
                {
                    // no reliable fuel level stored in model — keep 0 or compute if you have a tank log
                }
            }

            // --- Basic Stats ---
            // Total Miles Driven (use ActualDistance on Trip where driver)
            var totalMiles = await _context.Trips
                .AsNoTracking()
                .Where(t => t.DriverId == driver.Id && t.ActualDistance != null)
                .SumAsync(t => (decimal?)t.ActualDistance) ?? 0m;

            // Hours this month (sum of trip durations where actual times exist)
            var firstOfMonth = new DateTime(now.Year, now.Month, 1);
            var hoursThisMonth = await _context.Trips.AsNoTracking()
                .Where(t => t.DriverId == driver.Id
                            && t.ActualStartDate != null && t.ActualEndDate != null
                            && t.ActualStartDate >= firstOfMonth)
                .Select(t => EF.Functions.DateDiffMinute(t.ActualStartDate.Value, t.ActualEndDate.Value))
                .ToListAsync();

            double totalHours = hoursThisMonth.Sum() / 60.0;

            // Deliveries completed this month
            var deliveriesCompleted = await _context.Trips.AsNoTracking()
                .Where(t => t.DriverId == driver.Id
                            && t.Status == TripStatus.Completed
                            && t.ActualEndDate != null
                            && t.ActualEndDate >= firstOfMonth)
                .CountAsync();

            // Safety Score: simple formula: base 100, minus  (violations count * 3) clipped to [50,100]
            var recentViolationsCount = await _context.DriverViolations.AsNoTracking()
                .Where(v => v.DriverId == driver.Id)
                .CountAsync();

            var safetyScore = Math.Max(50, 100.0 - (recentViolationsCount * 3.0));

            dashboard.Stats = new DriverStatsDto
            {
                TotalMilesDriven = (double)totalMiles,
                HoursThisMonth = Math.Round(totalHours, 1),
                SafetyScorePercent = Math.Round(safetyScore, 1),
                DeliveriesCompleted = deliveriesCompleted
            };

            // --- Weekly Performance (last 7 days) ---
            var last7 = Enumerable.Range(0, 7)
                .Select(i => DateTime.UtcNow.Date.AddDays(-6 + i))
                .ToList();

            var tripsLast7 = await _context.Trips.AsNoTracking()
                .Where(t => t.DriverId == driver.Id && t.ActualDistance != null
                            && t.ActualEndDate != null
                            && t.ActualEndDate >= last7.First().Date)
                .Select(t => new { t.ActualEndDate, t.ActualDistance })
                .ToListAsync();

            var labels = new List<string>();
            var distances = new List<double>();

            foreach (var d in last7)
            {
                labels.Add(d.ToString("ddd dd"));
                var sum = tripsLast7
                    .Where(t => t.ActualEndDate.Value.Date == d.Date)
                    .Sum(t => (double?)t.ActualDistance) ?? 0.0;
                distances.Add(Math.Round(sum, 2));
            }

            dashboard.WeeklyPerformance = new WeeklyPerformanceDto
            {
                Labels = labels,
                Distances = distances
            };

            // --- Today's Schedule ---
            var today = DateTime.UtcNow.Date;
            var todaysTrips = await _context.Trips.AsNoTracking()
                .Where(t => t.DriverId == driver.Id
                            && t.ScheduledStartDate.Date == today)
                .OrderBy(t => t.ScheduledStartDate)
                .ToListAsync();

            dashboard.TodaysSchedule = todaysTrips.Select(t => new ScheduleItemDto
            {
                TripId = t.Id,
                ScheduledStart = t.ScheduledStartDate,
                TimeDisplay = t.ScheduledStartDate.ToString("HH:mm"),
                Title = $"{t.Origin} → {t.Destination}",
                ShortDescription = t.Purpose ?? string.Empty,
                Location = t.Origin,
                Status = t.Status.ToString()
            }).ToList();

            // --- Recent Activities (combine FuelLogs, Trips, DutyOfCare, Violations) ---
            var fuelLogs = await _context.FuelLogs.AsNoTracking()
                .Where(f => f.DriverId == driver.Id)
                .OrderByDescending(f => f.Date)
                .Take(5)
                .ToListAsync();

            var recentTrips = await _context.Trips.AsNoTracking()
                .Where(t => t.DriverId == driver.Id)
                .OrderByDescending(t => t.ModifiedDate ?? t.CreatedDate)
                .Take(5)
                .ToListAsync();

            var dutyOfCare = await _context.DriverDutyOfCares.AsNoTracking()
                .Where(d => d.DriverId == driver.Id)
                .OrderByDescending(d => d.Date)
                .Take(5)
                .ToListAsync();

            var violations = await _context.DriverViolations.AsNoTracking()
                .Where(v => v.DriverId == driver.Id)
                .OrderByDescending(v => v.CreatedDate)
                .Take(5)
                .ToListAsync();

            var activities = new List<ActivityItemDto>();

            activities.AddRange(fuelLogs.Select(f => new ActivityItemDto
            {
                OccurredAt = f.Date,
                Title = "Fuel Stop",
                Message = $"{f.Volume} refuelled @ odometer {f.Odometer}",
                Category = "Fuel"
            }));

            activities.AddRange(recentTrips.Select(t => new ActivityItemDto
            {
                OccurredAt = t.ModifiedDate ?? t.CreatedDate,
                Title = t.Status.ToString(),
                Message = $"{t.TripNumber ?? t.Id.ToString()}: {t.Origin} → {t.Destination}",
                Category = "Trip"
            }));

            activities.AddRange(dutyOfCare.Select(d => new ActivityItemDto
            {
                OccurredAt = d.Date,
                Title = d.DutyOfCareRecordType.ToString(),
                Message = $"Pre-check: {(d.VehiclePreCheckCompleted ? "Done" : "Pending")}",
                Category = "DutyOfCare"
            }));

            activities.AddRange(violations.Select(v => new ActivityItemDto
            {
                OccurredAt = v.CreatedDate,
                Title = "Violation",
                Message = v.Description ?? v.Notes,
                Category = "Violation"
            }));

            dashboard.RecentActivities = activities
                .OrderByDescending(a => a.OccurredAt)
                .Take(6)
                .ToList();

            // --- Safety Metrics ---
            // On-time delivery percent: completed trips this month where ActualEndDate <= ScheduledEndDate
            var completedThisMonth = await _context.Trips.AsNoTracking()
                .Where(t => t.DriverId == driver.Id && t.Status == TripStatus.Completed && t.ActualEndDate != null && t.ActualEndDate >= firstOfMonth)
                .ToListAsync();

            double onTimePercent = 100.0;
            if (completedThisMonth.Any())
            {
                var onTimeCount = completedThisMonth.Count(t => t.ActualEndDate <= t.ScheduledEndDate);
                onTimePercent = Math.Round((onTimeCount / (double)completedThisMonth.Count) * 100.0, 1);
            }

            // Speed compliance: approximate using trips without violations (if you track speed data, replace)
            var tripsCount = await _context.Trips.AsNoTracking().Where(t => t.DriverId == driver.Id && t.CreatedDate >= now.AddMonths(-3)).CountAsync();
            double speedCompliance = tripsCount == 0 ? 100.0 : Math.Round((1.0 - (recentViolationsCount / (double)Math.Max(1, tripsCount))) * 100.0, 1);
            speedCompliance = Math.Clamp(speedCompliance, 20.0, 100.0);

            // Days accident free: from Violations/incidents you can compute; we'll approximate:
            var lastAccident = await _context.DriverViolations.AsNoTracking()
                .Where(v => v.DriverId == driver.Id && v.Severity >= 3) // if Severity exists - else uses CreatedDate
                .OrderByDescending(v => v.CreatedDate)
                .FirstOrDefaultAsync();

            int daysAccidentFree = lastAccident == null ? int.MaxValue : (int)(DateTime.UtcNow - lastAccident.CreatedDate).TotalDays;
            if (daysAccidentFree == int.MaxValue) daysAccidentFree = (int)(DateTime.UtcNow - driver.CreatedDate).TotalDays; // fallback

            dashboard.SafetyMetrics = new SafetyMetricsDto
            {
                SafeDrivingScore = dashboard.Stats.SafetyScorePercent,
                SpeedCompliancePercent = speedCompliance,
                OnTimeDeliveryPercent = onTimePercent,
                DaysAccidentFree = Math.Min(daysAccidentFree, 9999),
                ViolationsCount = recentViolationsCount
            };

            // --- Monthly Performance last 6 months ---
            var monthlyLabels = new List<string>();
            var monthlyDistances = new List<double>();
            for (int i = 5; i >= 0; i--)
            {
                var dt = new DateTime(now.Year, now.Month, 1).AddMonths(-i);
                monthlyLabels.Add(dt.ToString("MMM yyyy"));

                var monthSum = await _context.Trips.AsNoTracking()
                    .Where(t => t.DriverId == driver.Id
                                && t.ActualDistance != null
                                && t.ActualEndDate != null
                                && t.ActualEndDate.Value.Year == dt.Year
                                && t.ActualEndDate.Value.Month == dt.Month)
                    .SumAsync(t => (decimal?)t.ActualDistance) ?? 0m;

                monthlyDistances.Add((double)monthSum);
            }

            dashboard.MonthlyPerformance = new MonthlyPerformanceDto
            {
                Labels = monthlyLabels,
                Distances = monthlyDistances,
                Achievements = new List<AchievementDto>
                {
                    new AchievementDto { Title = "Safety Champion", SubText = "Zero safety incidents (month)" },
                    new AchievementDto { Title = "On-Time Excellence", SubText = "95% delivery punctuality" }
                }
            };

            return new MessageResponse<DriverDashboardDto>
            {
                Success = true,
                Message = "Driver dashboard loaded",
                Result = dashboard
            };
        }
    }
}
