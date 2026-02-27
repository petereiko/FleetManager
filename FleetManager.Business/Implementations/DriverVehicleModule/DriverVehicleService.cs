using FleetManager.Business.Database.Entities;
using FleetManager.Business.DataObjects;
using FleetManager.Business.Enums;
using FleetManager.Business.Interfaces.DriverVehicleModule;
using FleetManager.Business.Interfaces.NotificationModule;
using FleetManager.Business.Interfaces.UserModule;
using FleetManager.Business.UtilityModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Implementations.DriverVehicleModule
{

    public class DriverVehicleService : IDriverVehicleService
    {
        private readonly FleetManagerDbContext _context;
        private readonly IAuthUser _authUser;
        private readonly ILogger<DriverVehicleService> _logger;
        private readonly INotificationService _notification;

        public DriverVehicleService(
            FleetManagerDbContext context,
            IAuthUser authUser,
            ILogger<DriverVehicleService> logger,
            INotificationService notification)
        {
            _context = context;
            _authUser = authUser;
            _logger = logger;
            _notification = notification;
        }

        private void EnsureAdminOrOwner()
        {
            var roles = (_authUser.Roles ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(r => r.Trim());

            if (!roles.Contains("Company Admin")
             && !roles.Contains("Company Owner")
             && !roles.Contains("Super Admin"))
            {
                throw new UnauthorizedAccessException("You do not have permission to manage assignments.");
            }
        }

        private static string ResolveVehicleDisplayName(Vehicle v)
        {
            var make = !string.IsNullOrWhiteSpace(v.CustomMakeName) ? v.CustomMakeName : (v.VehicleMake?.Name ?? "Unknown");
            var model = !string.IsNullOrWhiteSpace(v.CustomModelName) ? v.CustomModelName : (v.VehicleModel?.Name ?? "");
            return $"{make} {model}".Trim();
        }

        public async Task<MessageResponse<DriverVehicleDto>> AssignVehicleAsync(DriverVehicleDto dto, string createdBy)
        {
            EnsureAdminOrOwner();
            var resp = new MessageResponse<DriverVehicleDto>();
            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                // optional: check driver & vehicle exist
                var driver = await _context.Drivers.FindAsync(dto.DriverId);
                var vehicle = await _context.Vehicles.FindAsync(dto.VehicleId);
                if (driver == null || vehicle == null)
                {
                    resp.Message = "Driver or vehicle not found.";
                    return resp;
                }

                var entity = new DriverVehicle
                {
                    DriverId = dto.DriverId,
                    VehicleId = dto.VehicleId,
                    StartDate = dto.StartDate,
                    EndDate = dto.EndDate,
                    CreatedBy = createdBy,
                    CreatedDate = DateTime.UtcNow,
                    IsActive = true,
                    //CompanyId = _authUser.CompanyId,
                    //CompanyBranchId = _authUser.CompanyBranchId
                };

                _context.Add(entity);
                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                dto.Id = entity.Id;
                resp.Success = true;
                resp.Result = dto;


                // ── Notify the driver ────────────────────────────────────────
                if (!string.IsNullOrEmpty(driver.UserId))
                {
                    var title = "Vehicle Assigned Update";
                    var vehicleDisplay = ResolveVehicleDisplayName(vehicle);
                    var message = $"You have been assigned vehicle {vehicleDisplay} with license plate: {vehicle.PlateNo}. Await further Instructions";
                    await _notification.CreateAsync(driver.UserId, title, message, NotificationType.Vehicle, new
                    {
                        assignmentId = dto.Id,
                        vehicleId = dto.VehicleId
                    });
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to assign vehicle");
                await tx.RollbackAsync();
                resp.Message = "An error occurred while assigning vehicle.";
            }
            return resp;
        }

        public async Task<MessageResponse<DriverVehicleDto>> UpdateAssignmentAsync(DriverVehicleDto dto, string modifiedBy)
        {
            EnsureAdminOrOwner();
            var resp = new MessageResponse<DriverVehicleDto>();
            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                var entity = await _context.Set<DriverVehicle>().FindAsync(dto.Id);
                if (entity == null)
                {
                    resp.Message = "Assignment not found.";
                    return resp;
                }

                var driver = await _context.Drivers.FindAsync(dto.DriverId);
                var vehicle = await _context.Vehicles.FindAsync(dto.VehicleId);
                if (driver == null || vehicle == null)
                {
                    resp.Message = "Driver or vehicle not found.";
                    return resp;
                }

                entity.DriverId = dto.DriverId;
                entity.VehicleId = dto.VehicleId;
                entity.StartDate = dto.StartDate;
                entity.EndDate = dto.EndDate;
                entity.ModifiedBy = modifiedBy;
                entity.ModifiedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                resp.Success = true;
                resp.Result = dto;

                // ── Notify the driver of changes ────────────────────────────
                if (!string.IsNullOrEmpty(driver.UserId))
                {
                    var title = "Vehicle ssignment Update";
                    var vehicleDisplay = ResolveVehicleDisplayName(vehicle);
                    var message = $"Your vehicle assignment has been updated to {vehicleDisplay} " +
                                  $"(start {dto.StartDate:dd MMM yy}" +
                                  (dto.EndDate.HasValue ? $", end {dto.EndDate:dd MMM yy})." : ").");
                    await _notification.CreateAsync(driver.UserId, title, message, NotificationType.Vehicle, new
                    {
                        assignmentId = dto.Id,
                        vehicleId = dto.VehicleId
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update assignment");
                await tx.RollbackAsync();
                resp.Message = "An error occurred while updating assignment.";
            }
            return resp;
        }

        public async Task<MessageResponse> UnassignVehicleAsync(long assignmentId)
        {
            EnsureAdminOrOwner();
            var resp = new MessageResponse();
            try
            {
                var entity = await _context.Set<DriverVehicle>().FindAsync(assignmentId);
                if (entity == null)
                {
                    resp.Message = "Assignment not found.";
                    return resp;
                }

                var driver = await _context.Drivers.FindAsync(entity.DriverId);
                var vehicle = await _context.Vehicles.FindAsync(entity.VehicleId);

                _context.Remove(entity);
                await _context.SaveChangesAsync();
                resp.Success = true;

                if (driver != null && vehicle != null && !string.IsNullOrEmpty(driver.UserId))
                {
                    var title = "Vehicle Unassigned";
                    var vehicleDisplay = ResolveVehicleDisplayName(vehicle);
                    var message = $"You've been unassigned from operating vehicle: {vehicleDisplay} {vehicle.PlateNo}.";
                    await _notification.CreateAsync(driver.UserId, title, message, NotificationType.Vehicle, new
                    {
                        vehicleId = entity.VehicleId
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to unassign vehicle");
                resp.Message = "An error occurred while unassigning vehicle.";
            }
            return resp;
        }

        public IQueryable<DriverVehicleListItemDto> QueryAssignmentsByDriver(long driverId)
        {
            return _context.Set<DriverVehicle>()
                .AsNoTracking()
                .Where(dv => dv.DriverId == driverId)
                .Join(_context.Drivers.AsNoTracking(),
                      dv => dv.DriverId, d => d.Id, (dv, d) => new { dv, d })
                .Join(_context.Users.AsNoTracking(),
                      x => x.d.UserId, u => u.Id, (x, u) => new { x.dv, x.d, u })
                .Join(_context.Vehicles
                          .AsNoTracking()
                          .Include(v => v.VehicleMake)
                          .Include(v => v.VehicleModel),
                      x => x.dv.VehicleId, v => v.Id, (x, v) => new { x.dv, x.u, v })
                .Select(x => new DriverVehicleListItemDto
                {
                    Id = x.dv.Id,
                    DriverId = x.dv.DriverId!.Value,
                    DriverName = $"{x.u.FirstName} {x.u.LastName}",
                    VehicleId = x.dv.VehicleId!.Value,
                    // Custom name takes priority over catalogue name
                    VehicleMakeModel = x.v.CustomMakeName != null
                        ? (x.v.CustomMakeName + " " + x.v.CustomModelName).Trim()
                        : ((x.v.VehicleMake != null ? x.v.VehicleMake.Name : "") + " " +
                           (x.v.VehicleModel != null ? x.v.VehicleModel.Name : "")).Trim(),
                    PlateNo = x.v.PlateNo,
                    StartDate = x.dv.StartDate ?? DateTime.MinValue,
                    EndDate = x.dv.EndDate
                });
        }

        public IQueryable<DriverVehicleListItemDto> QueryAssignmentsByVehicle(long vehicleId)
        {
            EnsureAdminOrOwner();
            return _context.Set<DriverVehicle>()
                .AsNoTracking()
                .Where(dv => dv.VehicleId == vehicleId)
                .Join(_context.Drivers.AsNoTracking(),
                      dv => dv.DriverId, d => d.Id, (dv, d) => new { dv, d })
                .Join(_context.Users.AsNoTracking(),
                      x => x.d.UserId, u => u.Id, (x, u) => new { x.dv, x.d, u })
                .Join(_context.Vehicles
                          .AsNoTracking()
                          .Include(v => v.VehicleMake)
                          .Include(v => v.VehicleModel),
                      x => x.dv.VehicleId, v => v.Id, (x, v) => new { x.dv, x.u, v })
                .Select(x => new DriverVehicleListItemDto
                {
                    Id = x.dv.Id,
                    DriverId = x.dv.DriverId!.Value,
                    DriverName = $"{x.u.FirstName} {x.u.LastName}",
                    VehicleId = x.dv.VehicleId!.Value,
                    VehicleMakeModel = x.v.CustomMakeName != null
                        ? (x.v.CustomMakeName + " " + x.v.CustomModelName).Trim()
                        : ((x.v.VehicleMake != null ? x.v.VehicleMake.Name : "") + " " +
                           (x.v.VehicleModel != null ? x.v.VehicleModel.Name : "")).Trim(),
                    PlateNo = x.v.PlateNo,
                    StartDate = x.dv.StartDate ?? DateTime.MinValue,
                    EndDate = x.dv.EndDate
                });
        }


        public async Task<HashSet<long>> GetCurrentlyAssignedVehicleIdsAsync(long? excludeAssignmentId = null)
        {
            var today = DateTime.UtcNow.Date;

            var query = _context.Set<DriverVehicle>()
                .AsNoTracking()
                .Where(dv =>
                    dv.StartDate.HasValue &&
                    dv.StartDate.Value.Date <= today &&
                    (dv.EndDate == null || dv.EndDate.Value.Date >= today));

            if (excludeAssignmentId.HasValue)
                query = query.Where(dv => dv.Id != excludeAssignmentId.Value);

            var ids = await query
                .Where(dv => dv.VehicleId.HasValue)
                .Select(dv => dv.VehicleId!.Value)
                .Distinct()
                .ToListAsync();

            return new HashSet<long>(ids);
        }

        public async Task<long> GetDriverIdByUserAsync(string userId)
        {
            // no need to call EnsureAdminOrOwner here,
            // any logged‐in identity can call this; we’ll validate later.
            var drv = await _context.Drivers
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.UserId == userId);

            if (drv == null)
            {
                _logger.LogWarning("User {UserId} is not a driver.", userId);
                throw new UnauthorizedAccessException("You are not registered as a driver.");
            }

            return drv.Id;
        }
    }

}
