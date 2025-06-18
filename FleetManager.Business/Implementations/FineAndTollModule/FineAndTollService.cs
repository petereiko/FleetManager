using FleetManager.Business.Database.Entities;
using FleetManager.Business.DataObjects;
using FleetManager.Business.Enums;
using FleetManager.Business.Interfaces.FineAndTollModule;
using FleetManager.Business.Interfaces.NotificationModule;
using FleetManager.Business.Interfaces.UserModule;
using FleetManager.Business.UtilityModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Implementations.FineAndTollModule
{
    public class FineAndTollService : IFineAndTollService
    {
        private readonly FleetManagerDbContext _context;
        private readonly IAuthUser _auth;
        private readonly INotificationService _notification;
        private readonly ILogger<FineAndTollService> _logger;

        public FineAndTollService(
            FleetManagerDbContext context,
            IAuthUser authUser,
            ILogger<FineAndTollService> logger,
            INotificationService notification)
        {
            _context = context;
            _auth = authUser;
            _logger = logger;
            _notification = notification;
        }

        private void EnsureAdminOrOwner()
        {
            var roles = (_auth.Roles ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(r => r.Trim());

            if (!roles.Contains("Company Admin")
             && !roles.Contains("Company Owner")
             && !roles.Contains("Super Admin"))
            {
                throw new UnauthorizedAccessException("You do not have permission to manage fines and tolls.");
            }
        }

        // Admin: view all records in their branch
        public IQueryable<FineAndTollDto> QueryByBranch(long? branchId = null)
        {
            EnsureAdminOrOwner();
            var branch = branchId ?? _auth.CompanyBranchId;

            return _context.FineAndTolls.AsNoTracking()
                .Include(x => x.Driver)
                .Include(x => x.Vehicle)
                .Where(e => e.CompanyBranchId == branch)
                .Select(e => new FineAndTollDto
                {
                    Id = e.Id,
                    DriverId = e.DriverId,
                    DriverName = e.Driver.FirstName + " " + e.Driver.LastName,
                    VehicleId = e.VehicleId,
                    VehicleDescription = $"{e.Vehicle.Make} {e.Vehicle.Model} ({e.Vehicle.PlateNo.ToUpper()})",
                    Type = e.Type,
                    Title = e.Title,
                    Amount = e.Amount,
                    Currency = e.Currency,
                    Reason = e.Reason,
                    Notes = e.Notes,
                    IsMinimal = e.IsMinimal,
                    Status = e.Status,
                    PaidDate = e.PaidDate,
                    CreatedDate = e.CreatedDate,
                    CreatedBy = e.CreatedBy,
                    ModifiedDate = e.ModifiedDate,
                    ModifiedBy = e.ModifiedBy,
                    CompanyBranchId = e.CompanyBranchId
                });
        }

        // Driver: view own fines
        public IQueryable<FineAndTollDto> QueryByDriver(string driverUserId)
        {
            // driver sees only their own
            return _context.FineAndTolls.AsNoTracking()
                .Include(x => x.Driver)
                .Include(x => x.Vehicle)
                .Where(e => e.DriverId == driverUserId)
                .Select(e => new FineAndTollDto
                {
                    Id = e.Id,
                    DriverId = e.DriverId,
                    DriverName = e.Driver.FirstName + " " + e.Driver.LastName,
                    VehicleId = e.VehicleId,
                    VehicleDescription = $"{e.Vehicle.Make} {e.Vehicle.Model} ({e.Vehicle.PlateNo.ToUpper()})",
                    Type = e.Type,
                    Title = e.Title,
                    Amount = e.Amount,
                    Currency = e.Currency,
                    Reason = e.Reason,
                    Notes = e.Notes,
                    IsMinimal = e.IsMinimal,
                    Status = e.Status,
                    PaidDate = e.PaidDate,
                    CreatedDate = e.CreatedDate,
                    CreatedBy = e.CreatedBy,
                    ModifiedDate = e.ModifiedDate,
                    ModifiedBy = e.ModifiedBy,
                    CompanyBranchId = e.CompanyBranchId
                });
        }

        // Get by Id (for both)
        public async Task<FineAndTollDto?> GetByIdAsync(long id)
        {
            var e = await _context.FineAndTolls.AsNoTracking()
                .Include(x => x.Driver)
                .Include(x => x.Vehicle)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (e == null)
                return null;

            // enforce branch for admins
            if (!_auth.Roles.Split(',').Any(r => r.Trim() is "Super Admin" or "Company Owner" or "Company Admin")
                && e.DriverId != _auth.UserId)
            {
                throw new UnauthorizedAccessException();
            }

            return new FineAndTollDto
            {
                Id = e.Id,
                DriverId = e.DriverId,
                DriverName = e.Driver.FirstName + " " + e.Driver.LastName,
                VehicleId = e.VehicleId,
                VehicleDescription = $"{e.Vehicle.Make} {e.Vehicle.Model} ({e.Vehicle.PlateNo.ToUpper()})",
                Type = e.Type,
                Title = e.Title,
                Amount = e.Amount,
                Currency = e.Currency,
                Reason = e.Reason,
                Notes = e.Notes,
                IsMinimal = e.IsMinimal,
                Status = e.Status,
                PaidDate = e.PaidDate,
                CreatedDate = e.CreatedDate,
                CreatedBy = e.CreatedBy,
                ModifiedDate = e.ModifiedDate,
                ModifiedBy = e.ModifiedBy,
                CompanyBranchId = e.CompanyBranchId
            };
        }

        // Driver: create fine/toll
        public async Task<MessageResponse<FineAndTollDto>> CreateAsync(FineAndTollInputDto input, string createdByUserId)
        {
            var resp = new MessageResponse<FineAndTollDto>();
            try
            {
                // only drivers
                var roles = (_auth.Roles ?? "").Split(',').Select(r => r.Trim());
                if (!roles.Contains("Driver"))
                    throw new UnauthorizedAccessException("Only drivers can log fines/tolls.");

                // load driver entity to get their name
                var driver = await _context.Drivers
                                           .Include(d => d.User)
                                           .SingleAsync(d => d.UserId == createdByUserId);
                var driverName = $"{driver.User.FirstName} {driver.User.LastName}";


                var branchId = _auth.CompanyBranchId;
                var entity = new FineAndToll
                {
                    DriverId = createdByUserId,
                    VehicleId = input.VehicleId,
                    Type = input.Type,
                    Title = input.Title,
                    Amount = input.Amount,
                    Currency = input.Currency,
                    Reason = input.Reason,
                    Notes = input.Notes,
                    IsMinimal = input.IsMinimal,
                    Status = FineTollStatus.Unpaid,
                    PaidDate = input.IsMinimal ? DateTime.UtcNow : (DateTime?)null,
                    CompanyBranchId = branchId,
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = createdByUserId
                };

                _context.FineAndTolls.Add(entity);
                await _context.SaveChangesAsync();

                var dto = new FineAndTollDto
                {
                    Id = entity.Id,
                    DriverId = entity.DriverId,
                    VehicleId = entity.VehicleId,
                    VehicleDescription = "",
                    DriverName = driverName,
                    Type = entity.Type,
                    Title = entity.Title,
                    Amount = entity.Amount,
                    Currency = entity.Currency,
                    Reason = entity.Reason,
                    Notes = entity.Notes,
                    IsMinimal = entity.IsMinimal,
                    Status = entity.Status,
                    PaidDate = entity.PaidDate,
                    CreatedDate = entity.CreatedDate,
                    CreatedBy = entity.CreatedBy,
                    CompanyBranchId = entity.CompanyBranchId
                };

                resp.Success = true;
                resp.Result = dto;

                // notify branch admins
                var branchAdmins = await _context.CompanyAdmins
                    .Where(ca => ca.CompanyBranchId == branchId && ca.IsActive)
                    .Select(ca => ca.UserId)
                    .Where(id => !string.IsNullOrEmpty(id))
                    .ToListAsync();

                var notificationTitle = input.Type == FineTollType.Fine ? "New Fine Logged" : "New Toll Logged";
                var notificationMessage = $"{driverName} logged a {input.Type} fee of {entity.Amount} on {entity.CreatedDate:dd MMM yyyy} (Unpaid).";

                foreach (var adminId in branchAdmins)
                {
                    await _notification.CreateAsync(
                        adminId,
                        notificationTitle,
                        notificationMessage,
                        NotificationType.Info,
                        new { fineId = dto.Id }
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating FineAndToll record");
                resp.Message = "An unexpected error occurred while creating the record.";
            }

            return resp;
        }

        // Admin: change status to Paid
        public async Task<MessageResponse<FineAndTollDto>> UpdateStatusAsync(long id, FineTollStatus newStatus, string modifiedByUserId)
        {
            EnsureAdminOrOwner();
            var resp = new MessageResponse<FineAndTollDto>();

            try
            {
                var entity = await _context.FineAndTolls.FirstOrDefaultAsync(e => e.Id == id);
                if (entity == null)
                {
                    resp.Message = "Record not found.";
                    return resp;
                }

                entity.Status = newStatus;
                if (newStatus == FineTollStatus.Paid)
                    entity.PaidDate = DateTime.UtcNow;

                entity.ModifiedBy = modifiedByUserId;
                entity.ModifiedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                resp.Success = true;
                resp.Result = await GetByIdAsync(id)!;

                // notify driver
                var driverUserId = entity.DriverId;
                var notificationTitle = entity.Type == FineTollType.Fine ? "Fine Paid" : "Toll Paid";
                var notificationMessage = $"Your {entity.Type} (\"{entity.Title}\") has been marked {entity.Status}.";

                await _notification.CreateAsync(
                    driverUserId,
                    notificationTitle,
                    notificationMessage,
                    NotificationType.Success,
                    new { fineId = entity.Id }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating FineAndToll status Id {Id}", id);
                resp.Message = "An unexpected error occurred while updating the status.";
            }

            return resp;
        }

        public List<SelectListItem> GetFineTollTypeOptions()
        {
            return Enum.GetValues<FineTollType>()
                .Cast<FineTollType>()
                .Select(e => new SelectListItem
                {
                    Value = ((int)e).ToString(),
                    Text = e.ToString()
                })
                .ToList();
        }
        public List<SelectListItem> GetFineStatusOptions()
        {
            return Enum.GetValues<FineTollStatus>()
                .Cast<FineTollStatus>()
                .Select(e => new SelectListItem
                {
                    Value = ((int)e).ToString(),
                    Text = e.ToString()
                })
                .ToList();
        }
    }
}
