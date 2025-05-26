using FleetManager.Business.Database.Entities;
using FleetManager.Business.DataObjects;
using FleetManager.Business.Interfaces.DriverVehicleModule;
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

        public DriverVehicleService(
            FleetManagerDbContext context,
            IAuthUser authUser,
            ILogger<DriverVehicleService> logger)
        {
            _context = context;
            _authUser = authUser;
            _logger = logger;
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
                    IsActive = true
                };

                _context.Add(entity);
                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                dto.Id = entity.Id;
                resp.Success = true;
                resp.Result = dto;
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
                _context.Remove(entity);
                await _context.SaveChangesAsync();
                resp.Success = true;
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
            EnsureAdminOrOwner();
            return from dv in _context.Set<DriverVehicle>().AsNoTracking()
                   join d in _context.Drivers.AsNoTracking() on dv.DriverId equals d.Id
                   join u in _context.Users.AsNoTracking() on d.UserId equals u.Id
                   join v in _context.Vehicles.AsNoTracking() on dv.VehicleId equals v.Id
                   where dv.DriverId == driverId
                   select new DriverVehicleListItemDto
                   {
                       Id = dv.Id,
                       DriverId = dv.DriverId!.Value,
                       DriverName = $"{u.FirstName} {u.LastName}",
                       VehicleId = dv.VehicleId!.Value,
                       VehicleMakeModel = $"{v.Make} {v.Model}",
                       PlateNo = v.PlateNo,
                       StartDate = dv.StartDate ?? DateTime.MinValue,
                       EndDate = dv.EndDate
                   };
        }

        public IQueryable<DriverVehicleListItemDto> QueryAssignmentsByVehicle(long vehicleId)
        {
            EnsureAdminOrOwner();
            return from dv in _context.Set<DriverVehicle>().AsNoTracking()
                   join d in _context.Drivers.AsNoTracking() on dv.DriverId equals d.Id
                   join u in _context.Users.AsNoTracking() on d.UserId equals u.Id
                   join v in _context.Vehicles.AsNoTracking() on dv.VehicleId equals v.Id
                   where dv.VehicleId == vehicleId
                   select new DriverVehicleListItemDto
                   {
                       Id = dv.Id,
                       DriverId = dv.DriverId!.Value,
                       DriverName = $"{u.FirstName} {u.LastName}",
                       VehicleId = dv.VehicleId!.Value,
                       VehicleMakeModel = $"{v.Make} {v.Model}",
                       PlateNo = v.PlateNo,
                       StartDate = dv.StartDate ?? DateTime.MinValue,
                       EndDate = dv.EndDate
                   };
        }
    }

}
