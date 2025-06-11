using FleetManager.Business.Database.Entities;
using FleetManager.Business.DataObjects;
using FleetManager.Business.Enums;
using FleetManager.Business.Hubs;
using FleetManager.Business.Interfaces.FuelLogModule;
using FleetManager.Business.Interfaces.UserModule;
using FleetManager.Business.UtilityModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Implementations.FuelLogModule
{
    // Services/FuelLogService.cs
    public class FuelLogService : IFuelLogService
    {
        private readonly FleetManagerDbContext _context;
        private readonly IAuthUser _auth;
        private readonly ILogger<FuelLogService> _logger;
        private readonly IHubContext<NotificationHub> _hub;

        public FuelLogService(
            FleetManagerDbContext context,
            IAuthUser authUser,
            ILogger<FuelLogService> logger,
            IHubContext<NotificationHub> hub)
        {
            _context = context;
            _auth = authUser;
            _logger = logger;
            _hub = hub;
        }

        private void EnsureAdminOrOwner()
        {
            var roles = (_auth.Roles ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(r => r.Trim());
            if (!roles.Contains("Company Admin") &&
                !roles.Contains("Company Owner") &&
                !roles.Contains("Super Admin") &&
                !roles.Contains("Driver"))
            {
                throw new UnauthorizedAccessException("Insufficient permissions to access fuel logs.");
            }
        }

        public IQueryable<FuelLogDto> QueryByBranch(long? branchId = null)
        {
            EnsureAdminOrOwner();

            var q = from fl in _context.FuelLogs.AsNoTracking()
                    join d in _context.Drivers.AsNoTracking() on fl.DriverId equals d.Id
                    join u in _context.Users.AsNoTracking() on d.UserId equals u.Id
                    join v in _context.Vehicles.AsNoTracking() on fl.VehicleId equals v.Id
                    where !branchId.HasValue || d.CompanyBranchId == branchId.Value
                    select new FuelLogDto
                    {
                        Id = fl.Id,
                        DriverId = d.Id,
                        DriverName = (u.FirstName! + " " + u.LastName!).Trim(),
                        VehicleId = v.Id,
                        VehicleDescription = v.Make + " " + v.Model,
                        LicenseNo = v.PlateNo,
                        Date = fl.Date,
                        Odometer = fl.Odometer,
                        Volume = fl.Volume,
                        Cost = fl.Cost,
                        FuelType = fl.FuelType,
                        ReceiptPath = fl.ReceiptPath,
                        Notes = fl.Notes
                    };

            return q;
        }

        public async Task<FuelLogDto?> GetByIdAsync(long id)
        {
            return await QueryByBranch(_auth.CompanyBranchId)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<MessageResponse<FuelLogDto>> CreateAsync(FuelLogInputDto input, string createdByUserId)
        {
            var resp = new MessageResponse<FuelLogDto>();

            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                var entity = new FuelLog
                {
                    DriverId = input.DriverId,
                    VehicleId = input.VehicleId,
                    Date = input.Date,
                    Odometer = input.Odometer,
                    Volume = input.Volume,
                    Cost = input.Cost,
                    FuelType = input.FuelType,
                    Notes = input.Notes,
                    CreatedBy = createdByUserId,
                    CreatedDate = DateTime.UtcNow,
                    IsActive = true
                };

                _context.FuelLogs.Add(entity);
                await _context.SaveChangesAsync();

                // handle receipt file upload...
                if (input.ReceiptFile?.Length > 0)
                {
                    var uploadRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "FuelReceipts");
                    Directory.CreateDirectory(uploadRoot);

                    var fileName = $"{Guid.NewGuid()}_{input.ReceiptFile.FileName}";
                    var path = Path.Combine(uploadRoot, fileName);
                    await using var fs = new FileStream(path, FileMode.Create);
                    await input.ReceiptFile.CopyToAsync(fs);

                    entity.ReceiptPath = $"/FuelReceipts/{fileName}";
                    await _context.SaveChangesAsync();
                }

                await tx.CommitAsync();

                var dto = await GetByIdAsync(entity.Id)
                          ?? throw new InvalidOperationException("Newly created fuel log not found.");

                resp.Success = true;
                resp.Result = dto;

                // ── NOTIFICATION ────────────────────────────────────────────────
                // Who should get notified?
                // If creator is an Admin/Owner, notify the driver.
                // Otherwise (creator is the driver), notify their branch admin (CreatedByUserId on the branch).
                var creatorRoles = (_auth.Roles ?? "")
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(r => r.Trim());
                bool creatorIsAdmin = creatorRoles.Contains("Company Admin")
                                  || creatorRoles.Contains("Company Owner")
                                  || creatorRoles.Contains("Super Admin");

                if (creatorIsAdmin)
                {
                    // notify the driver
                    var driver = await _context.Drivers.FindAsync(input.DriverId);
                    if (driver?.UserId != null)
                    {
                        var notif = new
                        {
                            Title = "New Fuel Log",
                            Message = $"A new fuel entry on {dto.Date:dd MMM yyyy} for vehicle {dto.LicenseNo}.",
                            Timestamp = DateTime.UtcNow
                        };
                        await _hub.Clients.User(driver.UserId)
                                  .SendAsync("ReceiveNotification", notif);
                    }
                }
                else
                {
                    // creator is the driver—notify their branch admin
                    var driver = await _context.Drivers.FindAsync(input.DriverId);
                    if (driver?.CompanyBranchId != null)
                    {
                        // find the admin who onboarded or owns this branch
                        var branchAdmins = await _context.CompanyAdmins
                            .Where(ca => ca.CompanyBranchId == driver.CompanyBranchId && ca.IsActive)
                            .Select(ca => ca.UserId)
                            .ToListAsync();

                        var notif = new
                        {
                            Title = "Fuel Log Added",
                            Message = $"{dto.DriverName} logged fuel on {dto.Date:dd MMM yyyy}.",
                            Timestamp = DateTime.UtcNow
                        };

                        foreach (var adminUserId in branchAdmins.Where(id => !string.IsNullOrEmpty(id)))
                        {
                            await _hub.Clients.User(adminUserId!)
                                      .SendAsync("ReceiveNotification", notif);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating fuel log");
                await tx.RollbackAsync();
                resp.Message = "Failed to create fuel log.";
            }

            return resp;
        }
        public async Task<MessageResponse<FuelLogDto>> UpdateAsync(long id, FuelLogInputDto input, string modifiedByUserId)
        {
            var resp = new MessageResponse<FuelLogDto>();
            using var tx = await _context.Database.BeginTransactionAsync();

            try
            {
                var entity = await _context.FuelLogs.FindAsync(id);
                if (entity == null)
                {
                    resp.Message = "Fuel log not found.";
                    return resp;
                }

                entity.Date = input.Date;
                entity.Odometer = input.Odometer;
                entity.Volume = input.Volume;
                entity.Cost = input.Cost;
                entity.FuelType = input.FuelType;
                entity.Notes = input.Notes;
                entity.ModifiedBy = modifiedByUserId;
                entity.ModifiedDate = DateTime.UtcNow;

                // replace receipt if provided
                if (input.ReceiptFile != null && input.ReceiptFile.Length > 0)
                {
                    // delete old
                    if (!string.IsNullOrEmpty(entity.ReceiptPath))
                    {
                        var old = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", entity.ReceiptPath.TrimStart('/'));
                        if (File.Exists(old)) File.Delete(old);
                    }

                    var uploadRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "FuelReceipts");
                    Directory.CreateDirectory(uploadRoot);
                    var fileName = $"{Guid.NewGuid()}_{input.ReceiptFile.FileName}";
                    var path = Path.Combine(uploadRoot, fileName);
                    using var fs = new FileStream(path, FileMode.Create);
                    await input.ReceiptFile.CopyToAsync(fs);
                    entity.ReceiptPath = $"/FuelReceipts/{fileName}";
                }

                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                resp.Success = true;
                resp.Result = await GetByIdAsync(entity.Id)!;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating fuel log");
                await tx.RollbackAsync();
                resp.Message = "Failed to update fuel log.";
            }
            return resp;
        }

        public async Task<MessageResponse> DeleteAsync(long id)
        {
            var resp = new MessageResponse();
            try
            {
                var entity = await _context.FuelLogs.FindAsync(id);
                if (entity == null)
                {
                    resp.Message = "Fuel log not found.";
                    return resp;
                }

                // delete receipt file
                if (!string.IsNullOrEmpty(entity.ReceiptPath))
                {
                    var full = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", entity.ReceiptPath.TrimStart('/'));
                    if (File.Exists(full)) File.Delete(full);
                }

                _context.FuelLogs.Remove(entity);
                await _context.SaveChangesAsync();

                resp.Success = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting fuel log");
                resp.Message = "Failed to delete fuel log.";
            }
            return resp;
        }

        public List<SelectListItem> GetFuelTypeOptions()
        {
            return Enum.GetValues<FuelType>()
                .Cast<FuelType>()
                .Select(e => new SelectListItem
                {
                    Value = ((int)e).ToString(),
                    Text = e.ToString()
                })
                .ToList();
        }
    }

}
