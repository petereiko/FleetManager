using FleetManager.Business.DataObjects.ReportsDto;
using FleetManager.Business.Enums;
using FleetManager.Business.Interfaces.ReportModule;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Implementations.ReportModule
{
    public class ReportService : IReportService
    {
        private readonly FleetManagerDbContext _context;
        private readonly ILogger<ReportService> _logger;

        public ReportService(FleetManagerDbContext context, ILogger<ReportService> logger)
        {
            _context = context;
            _logger = logger;
        }

        //public async Task<ReportResult<DriverPerformanceReportDto>> GetDriverPerformanceReportAsync(ReportRequest request, long branchId)
        //{
        //    try
        //    {
        //        var query = _context.Drivers
        //            .Where(d => d.CompanyBranchId == branchId)
        //            .Include(d => d.User)
        //            .GroupJoin(
        //                _context.FuelLogWithPrev
        //                        .Where(f => f.Date >= request.StartDate && f.Date <= request.EndDate),
        //                    d => d.Id,
        //                    f => f.DriverId,
        //                    (d, logs) => new { Driver = d, FuelLogs = logs }
        //            )
        //            //.Select(x => new DriverPerformanceReportDto
        //            //{
        //            //    DriverId = x.Driver.Id,
        //            //    DriverName = x.Driver.User.FirstName + " " + x.Driver.User.LastName,
        //            //    LicenseNumber = x.Driver.LicenseNumber,
        //            //    LicenseExpiryDate = x.Driver.LicenseExpiryDate,
        //            //    IsLicenseExpiringSoon = x.Driver.LicenseExpiryDate.HasValue &&
        //            //                           x.Driver.LicenseExpiryDate.Value.AddDays(-30) <= DateTime.UtcNow,
        //            //    CurrentShiftStatus = x.Driver.ShiftStatus,
        //            //    LastSeen = x.Driver.LastSeen,
        //            //    TotalTrips = x.FuelLogs.Count(),
        //            //    TotalFuelCost = x.FuelLogs.Sum(f => f.Cost),
        //            //    TotalDistance = x.FuelLogs.Sum(f => (f.Odometer.HasValue && f.PreviousOdometer.HasValue) ? f.Odometer.Value - f.PreviousOdometer.Value: 0),
        //            //    AverageFuelConsumption = x.FuelLogs.Average(f => f.Volume),
        //            //    ActiveDays = x.FuelLogs.Select(f => f.Date.Date).Distinct().Count(),
        //            //    VehiclesAssigned = _context.DriverVehicles
        //            //        .Where(dv => dv.DriverId == x.Driver.Id)
        //            //        .Select(dv => dv.Vehicle.PlateNo)
        //            //        .ToList()
        //            //});
        //            .Select(x => new DriverPerformanceReportDto
        //            {
        //                DriverId = x.Driver.Id,
        //                DriverName = x.Driver.User.FirstName + " " + x.Driver.User.LastName,
        //                LicenseNumber = x.Driver.LicenseNumber,
        //                LicenseExpiryDate = x.Driver.LicenseExpiryDate,
        //                IsLicenseExpiringSoon = x.Driver.LicenseExpiryDate.HasValue &&
        //                    x.Driver.LicenseExpiryDate.Value.AddDays(-30) <= DateTime.UtcNow,
        //                CurrentShiftStatus = x.Driver.ShiftStatus,
        //                LastSeen = x.Driver.LastSeen,
        //                TotalTrips = x.FuelLogs.Count(),
        //                TotalFuelCost = x.FuelLogs.Sum(f => f.Cost ?? 0m),
        //                TotalDistance = x.FuelLogs.Sum(f => (f.Odometer ?? 0) - (f.PreviousOdometer ?? 0)),
        //                AverageFuelConsumption = x.FuelLogs.Where(f => f.Volume.HasValue).Select(f => f.Volume.Value).DefaultIfEmpty(0m).Average(),
        //                ActiveDays = x.FuelLogs.Select(f => f.Date.Date).Distinct().Count(),
        //                VehiclesAssigned = _context.DriverVehicles
        //.Where(dv => dv.DriverId == x.Driver.Id)
        //.Select(dv => dv.Vehicle.PlateNo)
        //.ToList()
        //            });


        //        // Calculate performance score in memory
        //        var data = await query.ToListAsync();
        //        foreach (var item in data)
        //        {
        //            item.PerformanceScore = CalculateDriverPerformance(item);
        //        }

        //        // Apply pagination
        //        var totalRecords = data.Count;
        //        var pagedData = data
        //            .Skip((request.PageNumber - 1) * request.PageSize)
        //            .Take(request.PageSize)
        //            .ToList();

        //        return new ReportResult<DriverPerformanceReportDto>
        //        {
        //            Data = pagedData,
        //            TotalRecords = totalRecords,
        //            PageNumber = request.PageNumber,
        //            PageSize = request.PageSize,
        //            ReportType = "Driver Performance Report",
        //            Summary = new Dictionary<string, object>
        //            {
        //                ["TotalDrivers"] = totalRecords,
        //                ["ActiveDrivers"] = data.Count(d => d.CurrentShiftStatus == ShiftStatus.OnDuty),
        //                ["DriversWithExpiredLicense"] = data.Count(d => d.IsLicenseExpiringSoon),
        //                ["AveragePerformanceScore"] = data.Average(d => d.PerformanceScore),
        //                ["TotalFuelCost"] = data.Sum(d => d.TotalFuelCost)
        //            },
        //            Request = request // ✅ Important fix
        //        };
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error generating driver performance report");
        //        throw;
        //    }
        //}

        public async Task<ReportResult<DriverPerformanceReportDto>> GetDriverPerformanceReportAsync(ReportRequest request, long branchId)
        {
            try
            {
                var startDate = request.StartDate;
                var endDate = request.EndDate;

                // Step 1: Pull all drivers in the branch
                var drivers = await _context.Drivers
                    .Where(d => d.CompanyBranchId == branchId)
                    .Include(d => d.User)
                    .ToListAsync();

                // Step 2: Pull fuel logs for those drivers within the date range
                var fuelLogs = await _context.FuelLogWithPrev
                    .Where(f => f.Date >= startDate && f.Date <= endDate)
                    .ToListAsync();

                // Step 3: Pull driver-vehicle assignments
                var driverVehicleMap = await _context.DriverVehicles
                    .Include(dv => dv.Vehicle)
                    .ToListAsync();

                // Step 4: Join and compute report data in memory
                var data = drivers.Select(driver =>
                {
                    var logs = fuelLogs.Where(f => f.DriverId == driver.Id).ToList();

                    var totalTrips = logs.Count;
                    var totalFuelCost = logs.Sum(f => f.Cost ?? 0);
                    var totalDistance = logs
                        .Where(f => f.Odometer.HasValue && f.PreviousOdometer.HasValue)
                        .Sum(f => f.Odometer.Value - f.PreviousOdometer.Value);

                    var avgFuelConsumption = logs.Any()
                        ? logs.Average(f => f.Volume ?? 0)
                        : 0;

                    var activeDays = logs
                        .Select(f => f.Date.Date)
                        .Distinct()
                        .Count();

                    var assignedVehicles = driverVehicleMap
                        .Where(dv => dv.DriverId == driver.Id && dv.Vehicle != null)
                        .Select(dv => dv.Vehicle.PlateNo)
                        .ToList();

                    var dto = new DriverPerformanceReportDto
                    {
                        DriverId = driver.Id,
                        DriverName = $"{driver.User.FirstName} {driver.User.LastName}",
                        LicenseNumber = driver.LicenseNumber,
                        LicenseExpiryDate = driver.LicenseExpiryDate,
                        IsLicenseExpiringSoon = driver.LicenseExpiryDate.HasValue &&
                                                 driver.LicenseExpiryDate.Value.AddDays(-30) <= DateTime.UtcNow,
                        CurrentShiftStatus = driver.ShiftStatus,
                        LastSeen = driver.LastSeen,
                        TotalTrips = totalTrips,
                        TotalFuelCost = totalFuelCost,
                        TotalDistance = totalDistance,
                        AverageFuelConsumption = avgFuelConsumption,
                        ActiveDays = activeDays,
                        VehiclesAssigned = assignedVehicles
                    };

                    dto.PerformanceScore = CalculateDriverPerformance(dto);
                    return dto;
                }).ToList();

                // Step 5: Apply pagination
                var totalRecords = data.Count;
                var pagedData = data
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToList();

                // Step 6: Build summary
                var summary = new Dictionary<string, object>
                {
                    ["TotalDrivers"] = totalRecords,
                    ["ActiveDrivers"] = data.Count(d => d.CurrentShiftStatus == ShiftStatus.OnDuty),
                    ["DriversWithExpiredLicense"] = data.Count(d => d.IsLicenseExpiringSoon),
                    ["AveragePerformanceScore"] = data.Any() ? Math.Round(data.Average(d => d.PerformanceScore), 2) : 0,
                    ["TotalFuelCost"] = data.Sum(d => d.TotalFuelCost)
                };

                return new ReportResult<DriverPerformanceReportDto>
                {
                    Data = pagedData,
                    TotalRecords = totalRecords,
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize,
                    ReportType = "Driver Performance Report",
                    Summary = summary,
                    Request = request
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating driver performance report");
                throw;
            }
        }



        public async Task<ReportResult<DriverComplianceReportDto>> GetDriverComplianceReportAsync(ReportRequest request, long branchId)
        {
            try
            {
                var query = _context.Drivers
                    .Where(d => d.CompanyBranchId == branchId)
                    .Include(d => d.User)
                    .Select(driver => new DriverComplianceReportDto
                    {
                        DriverId = driver.Id,
                        DriverName = driver.User.FirstName + " " + driver.User.LastName,
                        LicenseNumber = driver.LicenseNumber,
                        LicenseExpiryDate = driver.LicenseExpiryDate,
                        DaysUntilLicenseExpiry = driver.LicenseExpiryDate.HasValue ?
                            (int)(driver.LicenseExpiryDate.Value - DateTime.UtcNow).TotalDays : 0,
                        LicenseCategory = driver.LicenseCategory,
                        EmploymentStatus = driver.EmploymentStatus,
                        LastActiveDate = driver.LastSeen ?? DateTime.MinValue,
                        ComplianceIssues = _context.DriverViolations
                            .Where(v => v.DriverId == driver.Id &&
                                       v.ViolationDate >= request.StartDate)
                            .Select(v => v.Description)
                            .ToList()
                    });

                var totalRecords = await query.CountAsync();
                var data = await query
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync();

                // Calculate compliance status
                foreach (var item in data)
                {
                    item.IsCompliant = item.DaysUntilLicenseExpiry > 30 &&
                                      item.EmploymentStatus == EmploymentStatus.FullTime &&
                                      !item.ComplianceIssues.Any();
                }

                return new ReportResult<DriverComplianceReportDto>
                {
                    Data = data,
                    TotalRecords = totalRecords,
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize,
                    ReportType = "Driver Compliance Report",
                    Summary = new Dictionary<string, object>
                    {
                        ["TotalDrivers"] = totalRecords,
                        ["CompliantDrivers"] = data.Count(d => d.IsCompliant),
                        ["ExpiredLicenses"] = data.Count(d => d.DaysUntilLicenseExpiry <= 0),
                        ["LicensesExpiringSoon"] = data.Count(d => d.DaysUntilLicenseExpiry > 0 && d.DaysUntilLicenseExpiry <= 30)
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating driver compliance report");
                throw;
            }
        }

        public async Task<ReportResult<VehicleUtilizationReportDto>> GetVehicleUtilizationReportAsync(ReportRequest request, long branchId)
        {
            try
            {
                var query = _context.Vehicles
                    .Where(v => v.CompanyBranchId == branchId)
                    .Include(v => v.VehicleMake)
                    .Include(v => v.VehicleModel)
                    .GroupJoin(
                        _context.Trips
                            .Where(t => t.StartTime >= request.StartDate &&
                                       t.EndTime <= request.EndDate),
                        v => v.Id,
                        t => t.VehicleId,
                        (v, trips) => new { Vehicle = v, Trips = trips }
                    )
                    .Select(x => new VehicleUtilizationReportDto
                    {
                        VehicleId = x.Vehicle.Id,
                        VehicleIdentifier = $"{x.Vehicle.VehicleMake.Name} {x.Vehicle.VehicleModel.Name}",
                        Make = x.Vehicle.VehicleMake.Name,
                        Model = x.Vehicle.VehicleModel.Name,
                        Year = x.Vehicle.Year,
                        PlateNo = x.Vehicle.PlateNo,
                        Status = x.Vehicle.VehicleStatus,
                        TotalTrips = x.Trips.Count(),
                        TotalDistance = x.Trips.Sum(t => t.Distance),
                        UtilizationPercentage = (decimal)x.Trips.Sum(t => EF.Functions.DateDiffMinute(t.StartTime, t.EndTime ?? DateTime.UtcNow)) / (decimal)(request.EndDate - request.StartDate).TotalMinutes * 100,

                        LastServiceDate = x.Vehicle.LastServiceDate,
                        DaysSinceLastService = x.Vehicle.LastServiceDate.HasValue ? (int)(DateTime.UtcNow - x.Vehicle.LastServiceDate.Value).TotalDays : 0,
                        CurrentMileage = x.Vehicle.Mileage,
                        CurrentDriverName = _context.DriverVehicles
                            .Where(dv => dv.VehicleId == x.Vehicle.Id &&
                                        (dv.EndDate == null || dv.EndDate > DateTime.UtcNow))
                            .Select(dv => dv.Driver.User.FirstName + " " + dv.Driver.User.LastName)
                            .FirstOrDefault()
                    });

                var totalRecords = await query.CountAsync();
                var data = await query
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync();

                return new ReportResult<VehicleUtilizationReportDto>
                {
                    Data = data,
                    TotalRecords = totalRecords,
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize,
                    ReportType = "Vehicle Utilization Report",
                    Summary = new Dictionary<string, object>
                    {
                        ["TotalVehicles"] = totalRecords,
                        ["ActiveVehicles"] = data.Count(v => v.Status == VehicleStatus.Active),
                        ["AverageUtilization"] = data.Average(v => v.UtilizationPercentage),
                        ["TotalDistance"] = data.Sum(v => v.TotalDistance)
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating vehicle utilization report");
                throw;
            }
        }

        public async Task<ReportResult<VehicleMaintenanceReportDto>> GetVehicleMaintenanceReportAsync(ReportRequest request, long branchId)
        {
            try
            {
                var query = _context.Vehicles
                    .Where(v => v.CompanyBranchId == branchId)
                    .Include(v => v.VehicleMake)
                    .Include(v => v.VehicleModel)
                    .Select(v => new VehicleMaintenanceReportDto
                    {
                        VehicleId = v.Id,
                        VehicleIdentifier = $"{v.VehicleMake.Name} {v.VehicleModel.Name}",
                        Make = v.VehicleMake.Name,
                        Model = v.VehicleModel.Name,
                        PlateNo = v.PlateNo,
                        LastServiceDate = v.LastServiceDate,
                        DaysSinceLastService = v.LastServiceDate.HasValue ? (int)(DateTime.UtcNow - v.LastServiceDate.Value).TotalDays : 0,
                        CurrentMileage = v.Mileage,
                        InsuranceExpiryDate = v.InsuranceExpiryDate,
                        RoadWorthyExpiryDate = v.RoadWorthyExpiryDate,
                        Status = v.VehicleStatus,
                        MaintenanceAlerts = v.MaintenanceRecords
                            .Where(m => m.IsUrgent)
                            .Select(m => m.Description)
                            .ToList()
                    });

                var totalRecords = await query.CountAsync();
                var data = await query
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync();

                // Calculate maintenance flags
                foreach (var item in data)
                {
                    item.IsInsuranceExpiringSoon = item.InsuranceExpiryDate.HasValue &&
                        item.InsuranceExpiryDate.Value.AddDays(-30) <= DateTime.UtcNow;
                    item.IsRoadWorthyExpiringSoon = item.RoadWorthyExpiryDate.HasValue &&
                        item.RoadWorthyExpiryDate.Value.AddDays(-30) <= DateTime.UtcNow;
                    item.RequiresService = item.DaysSinceLastService > 90;
                }

                return new ReportResult<VehicleMaintenanceReportDto>
                {
                    Data = data,
                    TotalRecords = totalRecords,
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize,
                    ReportType = "Vehicle Maintenance Report",
                    Summary = new Dictionary<string, object>
                    {
                        ["VehiclesNeedingService"] = data.Count(v => v.RequiresService),
                        ["ExpiredInsurance"] = data.Count(v => v.InsuranceExpiryDate < DateTime.UtcNow),
                        ["UrgentMaintenanceItems"] = data.Sum(v => v.MaintenanceAlerts.Count)
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating vehicle maintenance report");
                throw;
            }
        }


        public async Task<ReportResult<FuelConsumptionReportDto>> GetFuelConsumptionReportAsync(ReportRequest request, long branchId)
        {
            try
            {
                var query = from log in _context.Set<FuelLogWithPrev>()
                            where log.Date >= request.StartDate &&
                                  log.Date <= request.EndDate &&
                                  log.CompanyBranchId == branchId
                            orderby log.Date descending
                            select new FuelConsumptionReportDto
                            {
                                VehicleId = log.VehicleId ?? 0,
                                VehicleIdentifier = log.VehicleIdentifier,
                                DriverName = log.DriverName ?? "Unassigned",
                                Date = log.Date,
                                Volume = log.Volume ?? 0m,
                                Cost = log.Cost ?? 0m,
                                Odometer = log.Odometer ?? 0,
                                PreviousOdometer = log.PreviousOdometer ?? 0,
                                PricePerLiter = log.Volume > 0 ? log.Cost / log.Volume ?? 0m : 0,
                                ConsumptionRate = (log.Odometer - log.PreviousOdometer) > 0
                                                      ? log.Volume * 100 / (log.Odometer - log.PreviousOdometer)
                                                      : 0,
                                FuelType = log.FuelType,
                                Notes = log.Notes
                            };

                var totalRecords = await query.CountAsync();
                var data = await query
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync();

                return new ReportResult<FuelConsumptionReportDto>
                {
                    Data = data,
                    TotalRecords = totalRecords,
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize,
                    ReportType = "Fuel Consumption Report",
                    Summary = new Dictionary<string, object>
                    {
                        ["TotalFillUps"] = totalRecords,
                        ["TotalVolume"] = data.Sum(f => f.Volume),
                        ["TotalCost"] = data.Sum(f => f.Cost),
                        ["AverageConsumption"] = data.Average(f => f.ConsumptionRate)
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating fuel consumption report");
                throw;
            }
        }

        public async Task<ReportResult<FuelEfficiencyReportDto>> GetFuelEfficiencyReportAsync(ReportRequest request, long branchId)
        {
            try
            {
                var query = _context.FuelLogs
                    .Where(f => f.Vehicle.CompanyBranchId == branchId &&
                               f.Date >= request.StartDate &&
                               f.Date <= request.EndDate)
                    .GroupBy(f => f.VehicleId)
                    .Select(g => new FuelEfficiencyReportDto
                    {
                        VehicleId = g.Key ?? 0,
                        VehicleIdentifier = g.First().Vehicle.VehicleMake.Name + " " +
                                         g.First().Vehicle.VehicleModel.Name,
                        Make = g.First().Vehicle.VehicleMake.Name,
                        Model = g.First().Vehicle.VehicleModel.Name,
                        AverageConsumption =
                            ((g.Sum(f => f.Volume) * 100m)
                            / ((g.Max(f => f.Odometer) ?? 0) - (g.Min(f => f.Odometer) ?? 0))),
                        TotalFuelCost = g.Sum(f => f.Cost),
                        TotalVolume = g.Sum(f => f.Volume),
                        TotalDistance = (g.Max(f => f.Odometer) ?? 0)
                                           - (g.Min(f => f.Odometer) ?? 0),
                        CostPerKm =
                            (g.Sum(f => f.Cost)
                            / ((g.Max(f => f.Odometer) ?? 0) - (g.Min(f => f.Odometer) ?? 0))),
                        TotalFillUps = g.Count(),
                        FirstFillUp = g.Min(f => f.Date),
                        LastFillUp = g.Max(f => f.Date)
                    })
                    .OrderByDescending(x => x.AverageConsumption);

                var totalRecords = await query.CountAsync();
                var data = await query
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync();

                // Calculate efficiency rating
                foreach (var item in data)
                {
                    item.EfficiencyRating = CalculateEfficiencyRating(item.AverageConsumption);
                }

                return new ReportResult<FuelEfficiencyReportDto>
                {
                    Data = data,
                    TotalRecords = totalRecords,
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize,
                    ReportType = "Fuel Efficiency Report",
                    Summary = new Dictionary<string, object>
                    {
                        ["BestEfficiency"] = data.Min(f => f.AverageConsumption),
                        ["WorstEfficiency"] = data.Max(f => f.AverageConsumption),
                        ["AverageCostPerKm"] = data.Average(f => f.CostPerKm)
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating fuel efficiency report");
                throw;
            }
        }

        public async Task<ReportResult<VehicleAssignmentReportDto>> GetVehicleAssignmentReportAsync(ReportRequest request, long branchId)
        {
            try
            {
                var query = _context.DriverVehicles
                    .Where(dv => dv.Vehicle.CompanyBranchId == branchId &&
                                ((dv.StartDate >= request.StartDate || dv.EndDate >= request.StartDate) &&
                                (dv.StartDate <= request.EndDate || dv.EndDate == null)))
                    .Include(dv => dv.Driver)
                    .ThenInclude(d => d.User)
                    .Include(dv => dv.Vehicle)
                    .ThenInclude(v => v.VehicleMake)
                    .Include(dv => dv.Vehicle)
                    .ThenInclude(v => v.VehicleModel)
                    .Select(dv => new VehicleAssignmentReportDto
                    {
                        AssignmentId = dv.Id,
                        DriverName = dv.Driver.User.FirstName + " " + dv.Driver.User.LastName,
                        VehicleIdentifier = $"{dv.Vehicle.VehicleMake.Name} {dv.Vehicle.VehicleModel.Name}",
                        StartDate = dv.StartDate,
                        EndDate = dv.EndDate,
                        IsActive = dv.EndDate == null,
                        AssignmentDuration = EF.Functions.DateDiffDay(dv.StartDate, dv.EndDate ?? DateTime.UtcNow).GetValueOrDefault(),
                        TotalTrips = _context.Trips
                            .Count(t => t.VehicleId == dv.VehicleId &&
                                       t.DriverId == dv.DriverId &&
                                       t.StartTime >= dv.StartDate &&
                                       (dv.EndDate == null || t.EndTime <= dv.EndDate)),
                        TotalDistance = _context.Trips
                            .Where(t => t.VehicleId == dv.VehicleId &&
                                        t.DriverId == dv.DriverId)
                            .Sum(t => t.Distance)
                    });

                var totalRecords = await query.CountAsync();
                var data = await query
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync();

                // Calculate performance score
                foreach (var item in data)
                {
                    item.PerformanceScore = CalculateAssignmentPerformance(item);
                }

                return new ReportResult<VehicleAssignmentReportDto>
                {
                    Data = data,
                    TotalRecords = totalRecords,
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize,
                    ReportType = "Vehicle Assignment Report",
                    Summary = new Dictionary<string, object>
                    {
                        ["ActiveAssignments"] = data.Count(a => a.IsActive),
                        ["AverageDuration"] = data.Average(a => a.AssignmentDuration),
                        ["TotalDistance"] = data.Sum(a => a.TotalDistance)
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating vehicle assignment report");
                throw;
            }
        }

        public async Task<Dictionary<string, object>> GetDashboardSummaryAsync(ReportRequest request, long branchId)
        {
            var summary = new Dictionary<string, object>();

            // Driver Summary
            var totalDrivers = await _context.Drivers
                .CountAsync(d => d.CompanyBranchId == branchId);

            var activeDrivers = await _context.Drivers
                .CountAsync(d => d.CompanyBranchId == branchId &&
                                d.ShiftStatus == ShiftStatus.OnDuty);

            var driversWithExpiringLicenses = await _context.Drivers
                .CountAsync(d => d.CompanyBranchId == branchId &&
                               d.LicenseExpiryDate.HasValue &&
                               d.LicenseExpiryDate.Value.AddDays(-30) <= DateTime.UtcNow);

            // Vehicle Summary
            var totalVehicles = await _context.Vehicles
                .CountAsync(v => v.CompanyBranchId == branchId);

            var activeVehicles = await _context.Vehicles
                .CountAsync(v => v.CompanyBranchId == branchId &&
                                v.VehicleStatus == VehicleStatus.Active);

            var vehiclesUnderMaintenance = await _context.Vehicles
                .CountAsync(v => v.CompanyBranchId == branchId &&
                                v.VehicleStatus == VehicleStatus.UnderMaintenance);

            // Fuel Summary (Last 30 Days)
            var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
            var totalFuelCost = await _context.FuelLogs
                .Where(f => f.Vehicle.CompanyBranchId == branchId &&
                           f.Date >= thirtyDaysAgo)
                .SumAsync(f => f.Cost);

            var totalFuelVolume = await _context.FuelLogs
                .Where(f => f.Vehicle.CompanyBranchId == branchId &&
                           f.Date >= thirtyDaysAgo)
                .SumAsync(f => f.Volume);

            var totalFillUps = await _context.FuelLogs
                .CountAsync(f => f.Vehicle.CompanyBranchId == branchId &&
                                f.Date >= thirtyDaysAgo);

            // Compliance Summary
            var expiredInsurance = await _context.Vehicles
                .CountAsync(v => v.CompanyBranchId == branchId &&
                               v.InsuranceExpiryDate.HasValue &&
                               v.InsuranceExpiryDate.Value <= DateTime.UtcNow);

            var expiredRoadWorthy = await _context.Vehicles
                .CountAsync(v => v.CompanyBranchId == branchId &&
                               v.RoadWorthyExpiryDate.HasValue &&
                               v.RoadWorthyExpiryDate.Value <= DateTime.UtcNow);

            summary.Add("DriverSummary", new
            {
                Total = totalDrivers,
                Active = activeDrivers,
                ExpiringLicenses = driversWithExpiringLicenses
            });

            summary.Add("VehicleSummary", new
            {
                Total = totalVehicles,
                Active = activeVehicles,
                UnderMaintenance = vehiclesUnderMaintenance
            });

            summary.Add("FuelSummary", new
            {
                TotalCost = totalFuelCost,
                TotalVolume = totalFuelVolume,
                TotalFillUps = totalFillUps
            });

            summary.Add("ComplianceSummary", new
            {
                ExpiredInsurance = expiredInsurance,
                ExpiredRoadWorthy = expiredRoadWorthy
            });

            return summary;
        }

        public async Task<List<ReportMetadata>> GetReportMetadataListAsync()
        {
            // You can extend this later to support role-based access or fetch from config/db
            var reports = new List<ReportMetadata>
            {
                new() { Name = "Driver Performance", Type = "DriverPerformance", Description = "Driver metrics and scores", LastGenerated = DateTime.UtcNow, EstimatedReadTimeMinutes = 3,CompletenessPercentage = 92},
                new() { Name = "Driver Compliance", Type = "DriverCompliance", Description = "License and regulation compliance" },
                new() { Name = "Vehicle Utilization", Type = "VehicleUtilization", Description = "Fleet usage statistics" },
                new() { Name = "Vehicle Maintenance", Type = "VehicleMaintenance", Description = "Service history and alerts" },
                new() { Name = "Fuel Consumption", Type = "FuelConsumption", Description = "Detailed fuel logs" },
                new() { Name = "Fuel Efficiency", Type = "FuelEfficiency", Description = "Mileage analysis by vehicle" },
                new() { Name = "Assignments", Type = "VehicleAssignment", Description = "Driver-vehicle assignment history" },
                new() { Name = "Dashboard", Type = "Dashboard", Description = "Key performance indicators" }
            };

            return await Task.FromResult(reports);
        }


        // Helper Methods
        private decimal CalculateDriverPerformance(DriverPerformanceReportDto dto)
        {
            decimal score = 100;

            if (dto.LicenseExpiryDate.HasValue && dto.LicenseExpiryDate.Value <= DateTime.UtcNow)
                score -= 30;
            else if (dto.IsLicenseExpiringSoon)
                score -= 10;

            if (dto.LastSeen.HasValue && (DateTime.UtcNow - dto.LastSeen.Value).TotalDays > 7)
                score -= 20;

            return Math.Max(0, Math.Min(100, score));
        }

        private int CalculateEfficiencyRating(decimal consumption)
        {
            return consumption switch
            {
                < 5 => 5,   // 5 stars for <5L/100km
                < 7 => 4,
                < 10 => 3,
                < 15 => 2,
                _ => 1      // 1 star for >15L/100km
            };
        }

        private decimal CalculateAssignmentPerformance(VehicleAssignmentReportDto dto)
        {
            return dto.AssignmentDuration > 0
               // Math.Min returns a double, so cast to decimal; use 0M for the decimal zero
               ? (decimal)Math.Min(100, (dto.TotalDistance / dto.AssignmentDuration) * 10)
               : 0M;
        }
    }
}
