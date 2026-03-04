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
                    .ThenInclude(v => v.VehicleMake)
                .Include(x => x.Vehicle)
                    .ThenInclude(v => v.VehicleModel)
                .Include(x => x.Attachments)
                .Where(e => e.CompanyBranchId == branch)
                .Select(e => new FineAndTollDto
                {
                    Id = e.Id,
                    DriverId = e.DriverId,
                    DriverName = e.Driver.FirstName + " " + e.Driver.LastName,
                    VehicleId = e.VehicleId,
                    VehicleDescription =
                        e.Vehicle.CustomMakeName != null
                            ? (e.Vehicle.CustomMakeName + " " + e.Vehicle.CustomModelName).Trim() + " " + e.Vehicle.PlateNo.ToUpper()
                            : (e.Vehicle.VehicleMake != null ? e.Vehicle.VehicleMake.Name : "Unknown")
                              + " "
                              + (e.Vehicle.VehicleModel != null ? e.Vehicle.VehicleModel.Name : "")
                              + " "
                              + e.Vehicle.PlateNo.ToUpper(),
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
                    CompanyBranchId = e.CompanyBranchId,
                    AttachmentPaths = e.Attachments.Select(a => a.FilePath).ToList()
                });
        }
        // Driver: view own fines
        public IQueryable<FineAndTollDto> QueryByDriver(string driverUserId)
        {
            return _context.FineAndTolls.AsNoTracking()
                .Include(x => x.Driver)
                .Include(x => x.Vehicle)
                    .ThenInclude(v => v.VehicleMake)
                .Include(x => x.Vehicle)
                    .ThenInclude(v => v.VehicleModel)
                .Include(x => x.Attachments)
                .Where(e => e.DriverId == driverUserId)
                .Select(e => new FineAndTollDto
                {
                    Id = e.Id,
                    DriverId = e.DriverId,
                    DriverName = e.Driver.FirstName + " " + e.Driver.LastName,
                    VehicleId = e.VehicleId,
                    VehicleDescription =
                        e.Vehicle.CustomMakeName != null
                            ? (e.Vehicle.CustomMakeName + " " + e.Vehicle.CustomModelName).Trim() + " " + e.Vehicle.PlateNo.ToUpper()
                            : (e.Vehicle.VehicleMake != null ? e.Vehicle.VehicleMake.Name : "Unknown")
                              + " "
                              + (e.Vehicle.VehicleModel != null ? e.Vehicle.VehicleModel.Name : "")
                              + " "
                              + e.Vehicle.PlateNo.ToUpper(),
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
                    CompanyBranchId = e.CompanyBranchId,
                    AttachmentPaths = e.Attachments.Select(a => a.FilePath).ToList()
                });
        }
        // Get by Id (for both)
        public async Task<FineAndTollDto?> GetByIdAsync(long id)
        {
            var e = await _context.FineAndTolls.AsNoTracking()
                .Include(x => x.Driver)
                .Include(x => x.Vehicle)
                    .ThenInclude(v => v.VehicleMake)
                .Include(x => x.Vehicle)
                    .ThenInclude(v => v.VehicleModel)
                .Include(x => x.Attachments)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (e == null) return null;

            return new FineAndTollDto
            {
                Id = e.Id,
                DriverId = e.DriverId,
                DriverName = e.Driver.FirstName + " " + e.Driver.LastName,
                VehicleId = e.VehicleId,
                VehicleDescription =
                    e.Vehicle.CustomMakeName != null
                        ? (e.Vehicle.CustomMakeName + " " + e.Vehicle.CustomModelName).Trim() + " " + e.Vehicle.PlateNo.ToUpper()
                        : (e.Vehicle.VehicleMake != null ? e.Vehicle.VehicleMake.Name : "Unknown")
                          + " "
                          + (e.Vehicle.VehicleModel != null ? e.Vehicle.VehicleModel.Name : "")
                          + " "
                          + e.Vehicle.PlateNo.ToUpper(),
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
                CompanyBranchId = e.CompanyBranchId,
                AttachmentPaths = e.Attachments.Select(a => a.FilePath).ToList()
            };
        }
        // Driver: create fine/toll
        public async Task<MessageResponse<FineAndTollDto>> CreateAsync(
            FineAndTollInputDto input,
            string createdByUserId)
        {
            var resp = new MessageResponse<FineAndTollDto>();
            using var tx = await _context.Database.BeginTransactionAsync();

            try
            {
                var roles = (_auth.Roles ?? "").Split(',').Select(r => r.Trim());
                if (!roles.Contains("Driver"))
                    throw new UnauthorizedAccessException("Only drivers can log fines/tolls.");

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
                    PaidDate = input.IsMinimal ? DateTime.UtcNow : null,
                    CompanyBranchId = branchId,
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = createdByUserId
                };

                _context.FineAndTolls.Add(entity);
                await _context.SaveChangesAsync();

                if (input.ProofFiles != null && input.ProofFiles.Any())
                {
                    var uploadRoot = Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot",
                        "FineTollProofs");

                    Directory.CreateDirectory(uploadRoot);

                    foreach (var file in input.ProofFiles)
                    {
                        if (file.Length <= 0) continue;

                        var fileName = $"{Guid.NewGuid()}_{file.FileName}";
                        var fullPath = Path.Combine(uploadRoot, fileName);

                        await using var fs = new FileStream(fullPath, FileMode.Create);
                        await file.CopyToAsync(fs);

                        _context.FineAndTollAttachments.Add(
                            new FineAndTollAttachment
                            {
                                FineAndTollId = entity.Id,
                                FileName = file.FileName,
                                FilePath = $"/FineTollProofs/{fileName}",
                                CreatedBy = createdByUserId,
                                CreatedDate = DateTime.UtcNow
                            });
                    }

                    await _context.SaveChangesAsync();
                }

                await tx.CommitAsync();

                resp.Success = true;
                resp.Result = await GetByIdAsync(entity.Id)!;
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _logger.LogError(ex, "Error creating FineAndToll record");
                resp.Message = "An unexpected error occurred while creating the record.";
            }

            return resp;
        }


        public async Task<MessageResponse<FineAndTollDto>> UpdateAsync(
    long id,
    FineAndTollInputDto input,
    string modifiedByUserId)
        {
            var resp = new MessageResponse<FineAndTollDto>();
            using var tx = await _context.Database.BeginTransactionAsync();

            try
            {
                var entity = await _context.FineAndTolls
                    .Include(x => x.Attachments)
                    .FirstOrDefaultAsync(x => x.Id == id);

                if (entity == null)
                {
                    resp.Message = "Record not found.";
                    return resp;
                }

                entity.Type = input.Type;
                entity.Title = input.Title;
                entity.Amount = input.Amount;
                entity.Currency = input.Currency;
                entity.Reason = input.Reason;
                entity.Notes = input.Notes;
                entity.IsMinimal = input.IsMinimal;
                entity.ModifiedBy = modifiedByUserId;
                entity.ModifiedDate = DateTime.UtcNow;

                // REMOVE selected attachments
                if (input.DeletedAttachmentIds != null && input.DeletedAttachmentIds.Any())
                {
                    var toDelete = entity.Attachments
                        .Where(a => input.DeletedAttachmentIds.Contains(a.Id))
                        .ToList();

                    foreach (var att in toDelete)
                    {
                        var full = Path.Combine(
                            Directory.GetCurrentDirectory(),
                            "wwwroot",
                            att.FilePath.TrimStart('/'));

                        if (File.Exists(full))
                            File.Delete(full);

                        _context.FineAndTollAttachments.Remove(att);
                    }
                }

                // ADD new files
                if (input.ProofFiles != null && input.ProofFiles.Any())
                {
                    var uploadRoot = Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot",
                        "FineTollProofs");

                    Directory.CreateDirectory(uploadRoot);

                    foreach (var file in input.ProofFiles)
                    {
                        if (file.Length <= 0) continue;

                        var fileName = $"{Guid.NewGuid()}_{file.FileName}";
                        var fullPath = Path.Combine(uploadRoot, fileName);

                        await using var fs = new FileStream(fullPath, FileMode.Create);
                        await file.CopyToAsync(fs);

                        _context.FineAndTollAttachments.Add(
                            new FineAndTollAttachment
                            {
                                FineAndTollId = entity.Id,
                                FileName = file.FileName,
                                FilePath = $"/FineTollProofs/{fileName}",
                                CreatedBy = modifiedByUserId,
                                CreatedDate = DateTime.UtcNow
                            });
                    }
                }

                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                resp.Success = true;
                resp.Result = await GetByIdAsync(id)!;
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _logger.LogError(ex, "Error updating FineAndToll record {Id}", id);
                resp.Message = "An error occurred while updating the record.";
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

        // Add to FineAndTollService.cs
        public async Task<MessageResponse> DeleteAsync(long id, string driverUserId)
        {
            var resp = new MessageResponse();

            try
            {
                var entity = await _context.FineAndTolls
                    .Include(x => x.Attachments)
                    .FirstOrDefaultAsync(e => e.Id == id);

                if (entity == null)
                {
                    resp.Message = "Fine/Toll record not found";
                    return resp;
                }

                if (entity.DriverId != driverUserId)
                    throw new UnauthorizedAccessException("You can only delete your own records");

                if (entity.Status == FineTollStatus.Paid)
                {
                    resp.Message = "Cannot delete a paid record";
                    return resp;
                }

                foreach (var att in entity.Attachments)
                {
                    var full = Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot",
                        att.FilePath.TrimStart('/'));

                    if (File.Exists(full))
                        File.Delete(full);
                }

                _context.FineAndTollAttachments.RemoveRange(entity.Attachments);
                _context.FineAndTolls.Remove(entity);

                await _context.SaveChangesAsync();

                resp.Success = true;
                resp.Message = "Record deleted successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting FineAndToll record {Id}", id);
                resp.Message = "An error occurred while deleting the record.";
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
