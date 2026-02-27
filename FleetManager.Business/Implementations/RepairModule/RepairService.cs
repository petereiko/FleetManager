using FleetManager.Business.Database.Entities.RepairHistory;
using FleetManager.Business.DataObjects.RepairDto;
using FleetManager.Business.DataObjects.RepairHistoryDto;
using FleetManager.Business.Enums;
using FleetManager.Business.Interfaces.NotificationModule;
using FleetManager.Business.Interfaces.RepairModule;
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

namespace FleetManager.Business.Implementations.RepairModule
{
    public class RepairService : IRepairService
    {
        private readonly FleetManagerDbContext _context;
        private readonly IAuthUser _auth;
        private readonly INotificationService _notification;
        private readonly ILogger<RepairService> _logger;

        public RepairService(
            FleetManagerDbContext context,
            IAuthUser authUser,
            ILogger<RepairService> logger,
            INotificationService notification)
        {
            _context = context;
            _auth = authUser;
            _logger = logger;
            _notification = notification;
        }

        private void EnsureAdminOrOwner()
        {
            var roles = (_auth.Roles ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries).Select(r => r.Trim());
            if (!roles.Contains("Company Admin") && !roles.Contains("Company Owner") && !roles.Contains("Super Admin"))
                throw new UnauthorizedAccessException("You do not have permission.");
        }

        public async Task<MessageResponse<PaginatedResult<RepairDto>>> QueryRepairsByBranchAsync(int page, int pageSize, long? branchId = null)
        {
            var resp = new MessageResponse<PaginatedResult<RepairDto>>();
            try
            {
                EnsureAdminOrOwner();

                var b = branchId ?? _auth.CompanyBranchId;
                var query = _context.Repairs.AsNoTracking()
                    .Include(r => r.Driver).ThenInclude(d => d.User)
                    .Include(r => r.Vehicle).ThenInclude(v => v.VehicleMake)
                    .Include(r => r.Vehicle).ThenInclude(v => v.VehicleModel)
                    .Include(r => r.Items).ThenInclude(i => i.VehiclePartCategory)
                    .Include(r => r.Items).ThenInclude(i => i.VehiclePart)
                    .Include(r => r.Invoice).ThenInclude(inv => inv.Items).ThenInclude(ii => ii.VehiclePart)
                    .Where(r => r.CompanyBranchId == b)
                    .OrderByDescending(r => r.CreatedDate)
                    .Select(r => new RepairDto
                    {
                        Id = r.Id,
                        VehicleId = r.VehicleId,
                        VehicleDescription = r.Vehicle.CustomMakeName != null ? (r.Vehicle.CustomMakeName + " " + r.Vehicle.CustomModelName).Trim() + " " + r.Vehicle.PlateNo.ToUpper()
                        : (r.Vehicle.VehicleMake != null ? r.Vehicle.VehicleMake.Name : "Unknown") + " " + (r.Vehicle.VehicleModel != null ? r.Vehicle.VehicleModel.Name : "") + " " + r.Vehicle.PlateNo.ToUpper(),
                        DriverId = r.DriverId,
                        DriverName = r.Driver != null ? r.Driver.User.FirstName + " " + r.Driver.User.LastName : null,
                        Subject = r.Subject,
                        Notes = r.Notes,
                        Status = r.Status,
                        Priority = r.Priority,
                        CreatedAt = r.CreatedDate,
                        ResolvedAt = r.ResolvedAt,
                        Items = r.Items.Select(i => new RepairItemDto
                        {
                            Id = i.Id,
                            PartId = i.VehiclePartId,
                            PartName = i.VehiclePart != null ? i.VehiclePart.Name : null,
                            PartCategoryName = i.VehiclePartCategory != null ? i.VehiclePartCategory.Name : null,
                            CustomDescription = i.CustomPartDescription,
                            Quantity = i.Quantity,
                            UnitPrice = i.UnitPrice,
                            LineTotal = i.Quantity * i.UnitPrice
                        }).ToList(),
                        Invoice = r.Invoice == null ? null : new RepairInvoiceDto
                        {
                            Id = r.Invoice.Id,
                            RepairId = r.Invoice.RepairId,
                            InvoiceDate = r.Invoice.InvoiceDate,
                            Status = r.Invoice.Status,
                            TotalAmount = r.Invoice.TotalAmount,
                            Items = r.Invoice.Items.Select(ii => new RepairInvoiceItemDto
                            {
                                Id = ii.Id,
                                PartId = ii.VehiclePartId,
                                PartName = ii.VehiclePart != null ? ii.VehiclePart.Name : null,
                                PartCategory = ii.VehiclePartCategory != null ? ii.VehiclePartCategory.Name : null,
                                Description = ii.Description,
                                Quantity = ii.Quantity,
                                UnitPrice = ii.UnitPrice,
                                LineTotal = ii.Quantity * ii.UnitPrice
                            }).ToList()
                        }
                    });

                var paged = await PaginatedResult<RepairDto>.CreateAsync(query, page, pageSize);
                resp.Success = true;
                resp.Result = paged;
            }
            catch (UnauthorizedAccessException uaEx)
            {
                _logger.LogWarning(uaEx, "Permission denied querying repairs by branch");
                resp.Message = uaEx.Message;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error querying repairs by branch");
                resp.Message = "Failed to load repairs.";
            }

            return resp;
        }

        public async Task<MessageResponse<PaginatedResult<RepairDto>>> QueryRepairsByVehicleAsync(int page, int pageSize, long vehicleId)
        {
            var resp = new MessageResponse<PaginatedResult<RepairDto>>();
            try
            {
                var query = _context.Repairs.AsNoTracking()
                   .Include(r => r.Driver).ThenInclude(d => d.User)
                   .Include(r => r.Vehicle).ThenInclude(v => v.VehicleMake)
                   .Include(r => r.Vehicle).ThenInclude(v => v.VehicleModel)
                   .Include(r => r.Items).ThenInclude(i => i.VehiclePartCategory)
                   .Include(r => r.Items).ThenInclude(i => i.VehiclePart)
                   .Include(r => r.Invoice).ThenInclude(inv => inv.Items)
                   .Where(r => r.VehicleId == vehicleId)
                   .OrderByDescending(r => r.CreatedDate)
                   .Select(r => new RepairDto
                   {
                       Id = r.Id,
                       VehicleId = r.VehicleId,
                       VehicleDescription = r.Vehicle.CustomMakeName != null ? (r.Vehicle.CustomMakeName + " " + r.Vehicle.CustomModelName).Trim() + " " + r.Vehicle.PlateNo.ToUpper()
                        : (r.Vehicle.VehicleMake != null ? r.Vehicle.VehicleMake.Name : "Unknown") + " " + (r.Vehicle.VehicleModel != null ? r.Vehicle.VehicleModel.Name : "") + " " + r.Vehicle.PlateNo.ToUpper(),
                       DriverId = r.DriverId,
                       DriverName = r.Driver != null ? r.Driver.User.FirstName + " " + r.Driver.User.LastName : null,
                       Subject = r.Subject,
                       Notes = r.Notes,
                       Status = r.Status,
                       Priority = r.Priority,
                       CreatedAt = r.CreatedDate,
                       ResolvedAt = r.ResolvedAt,
                       Items = r.Items.Select(i => new RepairItemDto
                       {
                           Id = i.Id,
                           PartId = i.VehiclePartId,
                           PartName = i.VehiclePart != null ? i.VehiclePart.Name : null,
                           PartCategoryName = i.VehiclePartCategory != null ? i.VehiclePartCategory.Name : null,
                           CustomDescription = i.CustomPartDescription,
                           Quantity = i.Quantity,
                           UnitPrice = i.UnitPrice,
                           LineTotal = i.Quantity * i.UnitPrice
                       }).ToList(),
                       Invoice = r.Invoice == null ? null : new RepairInvoiceDto
                       {
                           Id = r.Invoice.Id,
                           RepairId = r.Invoice.RepairId,
                           InvoiceDate = r.Invoice.InvoiceDate,
                           Status = r.Invoice.Status,
                           TotalAmount = r.Invoice.TotalAmount,
                           Items = r.Invoice.Items.Select(ii => new RepairInvoiceItemDto
                           {
                               Id = ii.Id,
                               PartId = ii.VehiclePartId,
                               PartName = ii.VehiclePart != null ? ii.VehiclePart.Name : null,
                               PartCategory = ii.VehiclePartCategory != null ? ii.VehiclePartCategory.Name : null,
                               Description = ii.Description,
                               Quantity = ii.Quantity,
                               UnitPrice = ii.UnitPrice,
                               LineTotal = ii.Quantity * ii.UnitPrice
                           }).ToList()
                       }
                   });

                var paged = await PaginatedResult<RepairDto>.CreateAsync(query, page, pageSize);
                resp.Success = true;
                resp.Result = paged;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error querying repairs by vehicle");
                resp.Message = "Failed to load repairs.";
            }
            return resp;
        }

        public async Task<RepairDto?> GetRepairByIdAsync(long repairId)
        {
            var r = await _context.Repairs.AsNoTracking()
                .Include(x => x.Driver).ThenInclude(d => d.User)
                .Include(x => x.Vehicle).ThenInclude(v => v.VehicleMake)
                .Include(x => x.Vehicle).ThenInclude(v => v.VehicleModel)
                // include Company + Branch from Vehicle
                .Include(t => t.Vehicle).ThenInclude(v => v.Company)
                .Include(t => t.Vehicle).ThenInclude(v => v.CompanyBranch)
                .Include(x => x.Items).ThenInclude(i => i.VehiclePartCategory)
                .Include(x => x.Items).ThenInclude(i => i.VehiclePart)
                .Include(x => x.Invoice).ThenInclude(inv => inv.Items).ThenInclude(ii => ii.VehiclePart)
                .FirstOrDefaultAsync(x => x.Id == repairId);

            if (r == null) return null;

            

            // company/branch shortcuts (safe)
            var company = r.Vehicle?.Company;
            var branch = r.Vehicle?.CompanyBranch;
            string? branchStateName = branch?.State?.Name;

            if (branchStateName == null && branch?.StateId != null)
            {
                var state = await _context.States
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Id == branch.StateId);
                branchStateName = state?.Name;
            }

            return new RepairDto
            {
                Id = r.Id,
                VehicleId = r.VehicleId,
                VehicleDescription = r.Vehicle.CustomMakeName != null ? (r.Vehicle.CustomMakeName + " " + r.Vehicle.CustomModelName).Trim() + " " + r.Vehicle.PlateNo.ToUpper()
                        : (r.Vehicle.VehicleMake != null ? r.Vehicle.VehicleMake.Name : "Unknown") + " " + (r.Vehicle.VehicleModel != null ? r.Vehicle.VehicleModel.Name : "") + " " + r.Vehicle.PlateNo.ToUpper(),
                DriverId = r.DriverId,
                DriverName = r.Driver != null ? $"{r.Driver.User.FirstName} {r.Driver.User.LastName}" : null,
                Subject = r.Subject,
                Notes = r.Notes,
                Status = r.Status,
                Priority = r.Priority,
                CreatedAt = r.CreatedDate,
                ResolvedAt = r.ResolvedAt,
                // NEW: company & branch info
                CompanyName = company?.Name,
                CompanyLogoUrl = company?.LogoUrl,
                CompanyEmail = company?.Email,
                CompanyPhone = company?.PhoneNumber,
                BranchName = branch?.Name,
                BranchAddress = branch?.Address,
                BranchState = branchStateName,
                BranchPhone = branch?.Phone,
                BranchEmail = branch?.Email,
                IsBranchHeadOffice = branch?.IsHeadOffice ?? false,
                Items = r.Items.Select(i => new RepairItemDto
                {
                    Id = i.Id,
                    PartId = i.VehiclePartId,
                    PartName = i.VehiclePart != null ? i.VehiclePart.Name : null,
                    PartCategoryName = i.VehiclePartCategory != null ? i.VehiclePartCategory.Name : null,
                    CustomDescription = i.CustomPartDescription,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    LineTotal = i.Quantity * i.UnitPrice
                }).ToList(),
                Invoice = r.Invoice == null ? null : new RepairInvoiceDto
                {
                    Id = r.Invoice.Id,
                    RepairId = r.Invoice.RepairId,
                    InvoiceDate = r.Invoice.InvoiceDate,
                    Status = r.Invoice.Status,
                    TotalAmount = r.Invoice.TotalAmount,
                    Items = r.Invoice.Items.Select(ii => new RepairInvoiceItemDto
                    {
                        Id = ii.Id,
                        PartId = ii.VehiclePartId,
                        PartName = ii.VehiclePart != null ? ii.VehiclePart.Name : null,
                        PartCategory = ii.VehiclePartCategory != null ? ii.VehiclePartCategory.Name : null,
                        Description = ii.Description,
                        Quantity = ii.Quantity,
                        UnitPrice = ii.UnitPrice,
                        LineTotal = ii.Quantity * ii.UnitPrice
                    }).ToList()
                }
            };
        }

        public async Task<MessageResponse<RepairDto>> CreateRepairAsync(RepairInputDto input, string createdByUserId)
        {
            var resp = new MessageResponse<RepairDto>();
            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                var branchId = _auth.CompanyBranchId;

                var repair = new Repair
                {
                    VehicleId = input.VehicleId,
                    DriverId = input.DriverId,
                    CompanyBranchId = branchId,
                    Subject = input.Subject,
                    Notes = input.Notes,
                    Status = RepairStatus.Pending,
                    Priority = input.Priority,
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = createdByUserId
                };

                _context.Repairs.Add(repair);
                await _context.SaveChangesAsync();

                foreach (var item in input.Items)
                {
                    _context.RepairItems.Add(new RepairItem
                    {
                        RepairId = repair.Id,
                        VehiclePartCategoryId = item.PartCategoryId,
                        VehiclePartId = item.PartId,
                        CustomPartDescription = item.CustomDescription,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice
                    });
                }

                await _context.SaveChangesAsync();

                // create repair invoice
                var total = input.Items.Sum(i => i.Quantity * i.UnitPrice);
                var inv = new RepairInvoice
                {
                    RepairId = repair.Id,
                    CompanyBranchId = branchId,
                    InvoiceDate = DateTime.UtcNow,
                    Status = InvoiceStatus.Pending,
                    TotalAmount = total,
                    CreatedBy = createdByUserId
                };
                _context.RepairInvoices.Add(inv);
                await _context.SaveChangesAsync();

                foreach (var item in input.Items)
                {
                    _context.RepairInvoiceItems.Add(new RepairInvoiceItem
                    {
                        RepairInvoiceId = inv.Id,
                        Description = item.CustomDescription,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        VehiclePartId = item.PartId,
                        VehiclePartCategoryId = item.PartCategoryId
                    });
                }
                await _context.SaveChangesAsync();

                await tx.CommitAsync();

                var dto = await GetRepairByIdAsync(repair.Id);
                resp.Success = true;
                resp.Result = dto;

                // notify admins in branch
                var admins = await _context.CompanyAdmins
                    .Where(a => a.CompanyBranchId == repair.CompanyBranchId && a.IsActive)
                    .Select(a => a.UserId).ToListAsync();

                foreach (var adminId in admins)
                {
                    await _notification.CreateAsync(
                        adminId,
                        "New Repair Request Logged",
                        $"New repair #{repair.Id} created for vehicle operated by {repair.Driver.User.FirstName}.",
                        NotificationType.Info,
                        new { repairId = repair.Id });
                }
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _logger.LogError(ex, "Error creating repair");
                resp.Message = "Failed to create repair.";
            }

            return resp;
        }

        public async Task<MessageResponse<RepairDto>> UpdateRepairAsync(UpdateRepairInputDto input)
        {
            EnsureAdminOrOwner();
            var resp = new MessageResponse<RepairDto>();
            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                // load repair + items + invoice + invoice.items
                var repair = await _context.Repairs
                    .Include(r => r.Items)
                    .Include(r => r.Invoice).ThenInclude(inv => inv.Items)
                    .FirstOrDefaultAsync(r => r.Id == input.RepairId);

                if (repair == null)
                {
                    resp.Message = "Repair not found";
                    return resp;
                }

                // update main fields
                repair.VehicleId = input.VehicleId;
                repair.DriverId = input.DriverId;
                repair.Subject = input.Subject;
                repair.Notes = input.Notes;
                repair.Priority = input.Priority;
                repair.ModifiedDate = DateTime.UtcNow;
                repair.ModifiedBy = _auth.UserId;

                // ----- SYNC ITEMS -----
                // Build dictionary of existing items by id for quick lookup
                var existingItems = repair.Items.ToDictionary(i => i.Id, i => i);

                // Track incoming item ids so we can remove ones not present
                var incomingIds = new HashSet<long>();

                foreach (var incoming in input.Items)
                {
                    if (incoming.Id.HasValue && existingItems.TryGetValue(incoming.Id.Value, out var existing))
                    {
                        // update existing
                        existing.VehiclePartCategoryId = incoming.PartCategoryId;
                        existing.VehiclePartId = incoming.PartId;
                        existing.CustomPartDescription = incoming.CustomDescription;
                        existing.Quantity = incoming.Quantity;
                        existing.UnitPrice = incoming.UnitPrice;
                        existing.ModifiedDate = DateTime.UtcNow;
                        existing.ModifiedBy = _auth.UserId;

                        incomingIds.Add(existing.Id);
                    }
                    else
                    {
                        // create new item
                        var newItem = new RepairItem
                        {
                            RepairId = repair.Id,
                            VehiclePartCategoryId = incoming.PartCategoryId,
                            VehiclePartId = incoming.PartId,
                            CustomPartDescription = incoming.CustomDescription,
                            Quantity = incoming.Quantity,
                            UnitPrice = incoming.UnitPrice,
                            CreatedDate = DateTime.UtcNow,
                            CreatedBy = _auth.UserId
                        };
                        _context.RepairItems.Add(newItem);
                        // We can't add to incomingIds now because id will be assigned after SaveChanges.
                    }
                }

                // Remove items that were deleted on client
                var toRemove = repair.Items.Where(i => !incomingIds.Contains(i.Id)).ToList();
                foreach (var rem in toRemove)
                {
                    _context.RepairItems.Remove(rem);
                }

                await _context.SaveChangesAsync(); // persist item adds/updates/deletes so new Ids exist

                // Re-load repair items to compute invoice
                var finalItems = await _context.RepairItems
                    .Where(i => i.RepairId == repair.Id)
                    .ToListAsync();

                // ----- SYNC / REBUILD INVOICE ITEMS & TOTAL -----
                // If there is no invoice, create one
                var invoice = repair.Invoice;
                if (invoice == null)
                {
                    invoice = new RepairInvoice
                    {
                        RepairId = repair.Id,
                        CompanyBranchId = repair.CompanyBranchId,
                        InvoiceDate = DateTime.UtcNow,
                        Status = InvoiceStatus.Pending,
                        CreatedBy = _auth.UserId,
                        TotalAmount = 0m
                    };
                    _context.RepairInvoices.Add(invoice);
                    await _context.SaveChangesAsync();
                }

                // Remove existing invoice items and recreate from finalItems (simpler & consistent)
                var existingInvoiceItems = await _context.RepairInvoiceItems
                    .Where(ii => ii.RepairInvoiceId == invoice.Id)
                    .ToListAsync();

                if (existingInvoiceItems.Any())
                {
                    _context.RepairInvoiceItems.RemoveRange(existingInvoiceItems);
                    await _context.SaveChangesAsync();
                }

                decimal total = 0m;
                foreach (var fi in finalItems)
                {
                    var ii = new RepairInvoiceItem
                    {
                        RepairInvoiceId = invoice.Id,
                        VehiclePartCategoryId = fi.VehiclePartCategoryId,
                        VehiclePartId = fi.VehiclePartId,
                        Description = fi.CustomPartDescription,
                        Quantity = fi.Quantity,
                        UnitPrice = fi.UnitPrice,
                        CreatedDate = DateTime.UtcNow,
                        CreatedBy = _auth.UserId
                    };
                    _context.RepairInvoiceItems.Add(ii);
                    total += fi.Quantity * fi.UnitPrice;
                }

                invoice.TotalAmount = total;
                invoice.ModifiedDate = DateTime.UtcNow;
                invoice.ModifiedBy = _auth.UserId;

                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                // Return fresh DTO using existing helper
                resp.Success = true;
                resp.Result = await GetRepairByIdAsync(repair.Id);
                resp.Message = "Repair updated successfully";

                // notify driver/admins if desired
                if (repair.DriverId.HasValue)
                {
                    var driver = await _context.Drivers.Include(d => d.User).FirstOrDefaultAsync(d => d.Id == repair.DriverId.Value);
                    if (driver != null)
                    {
                        await _notification.CreateAsync(
                            driver.UserId,
                            $"Repair #{repair.Id} Updated",
                            $"Repair details were updated by {_auth.UserId}",
                            NotificationType.Info,
                            new { repairId = repair.Id });
                    }
                }
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _logger.LogError(ex, "Error updating repair {RepairId}", input.RepairId);
                resp.Message = "Failed to update repair.";
            }

            return resp;
        }


        public async Task<MessageResponse<RepairDto>> UpdateRepairStatusAsync(UpdateRepairStatusDto input)
        {
            EnsureAdminOrOwner();
            var resp = new MessageResponse<RepairDto>();
            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                var repair = await _context.Repairs
                    .Include(r => r.Invoice)
                    .Include(r => r.Driver).ThenInclude(d => d.User)
                    .FirstOrDefaultAsync(r => r.Id == input.RepairId);

                if (repair == null) { resp.Message = "Repair not found"; return resp; }

                // handle reject => cancel invoice
                if (input.NewStatus == RepairStatus.Rejected && repair.Invoice != null)
                {
                    repair.Invoice.Status = InvoiceStatus.Cancelled;
                    repair.Invoice.ModifiedDate = DateTime.UtcNow;
                    repair.Invoice.ModifiedBy = _auth.UserId;

                    await _notification.CreateAsync(
                        repair.Driver!.UserId,
                        "Repair Invoice Cancelled",
                        $"Repair invoice #{repair.Invoice.Id} cancelled due to repair rejection.",
                        NotificationType.Warning,
                        new { invoiceId = repair.Invoice.Id });
                }

                if (input.InvoiceStatus.HasValue && repair.Invoice != null)
                {
                    repair.Invoice.Status = input.InvoiceStatus.Value;
                    repair.Invoice.ModifiedDate = DateTime.UtcNow;
                    repair.Invoice.ModifiedBy = _auth.UserId;

                    if (input.InvoiceStatus == InvoiceStatus.Paid)
                        repair.Status = RepairStatus.InProgress;
                }

                repair.Status = input.NewStatus;
                repair.ModifiedDate = DateTime.UtcNow;
                repair.ModifiedBy = _auth.UserId;
                if (input.NewStatus == RepairStatus.Resolved)
                    repair.ResolvedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                // notify driver
                if (repair.Driver != null)
                {
                    await _notification.CreateAsync(
                        repair.Driver.UserId,
                        $"Repair #{repair.Id} Updated",
                        $"Repair status is now {repair.Status} and invoice status {repair.Invoice?.Status}",
                        NotificationType.Info,
                        new { repairId = repair.Id });
                }

                resp.Success = true;
                resp.Result = await GetRepairByIdAsync(repair.Id);
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _logger.LogError(ex, "Error updating repair status {RepairId}", input.RepairId);
                resp.Message = "Failed to update repair status.";
            }
            return resp;
        }

        
        public async Task<MessageResponse<PaginatedResult<RepairInvoiceDto>>> QueryRepairInvoicesByBranchAsync(int page, int pageSize, long? branchId = null)
        {
            var resp = new MessageResponse<PaginatedResult<RepairInvoiceDto>>();
            try
            {
                EnsureAdminOrOwner();
                var b = branchId ?? _auth.CompanyBranchId;
                var query = _context.RepairInvoices.AsNoTracking()
                    .Include(inv => inv.Repair).ThenInclude(r => r.Driver).ThenInclude(d => d.User)
                    .Include(inv => inv.Repair).ThenInclude(r => r.Vehicle)
                    .Where(inv => inv.CompanyBranchId == b)
                    .OrderByDescending(inv => inv.InvoiceDate)
                    .Select(inv => new RepairInvoiceDto
                    {
                        Id = inv.Id,
                        RepairId = inv.RepairId,
                        InvoiceDate = inv.InvoiceDate,
                        Status = inv.Status,
                        TotalAmount = inv.TotalAmount,
                        Items = inv.Items.Select(ii => new RepairInvoiceItemDto
                        {
                            Id = ii.Id,
                            PartId = ii.VehiclePartId,
                            PartName = ii.VehiclePart != null ? ii.VehiclePart.Name : null,
                            PartCategory = ii.VehiclePartCategory != null ? ii.VehiclePartCategory.Name : null,
                            Description = ii.Description,
                            Quantity = ii.Quantity,
                            UnitPrice = ii.UnitPrice,
                            LineTotal = ii.Quantity * ii.UnitPrice
                        }).ToList()
                    });

                var paged = await PaginatedResult<RepairInvoiceDto>.CreateAsync(query, page, pageSize);
                resp.Success = true;
                resp.Result = paged;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error querying repair invoices");
                resp.Message = "Failed to load repair invoices.";
            }
            return resp;
        }

        public async Task<RepairInvoiceDto?> GetRepairInvoiceByIdAsync(long invoiceId)
        {
            var inv = await _context.RepairInvoices.AsNoTracking()
                .Include(i => i.Repair).ThenInclude(r => r.Driver).ThenInclude(d => d.User)
                .Include(i => i.Repair).ThenInclude(r => r.Vehicle)
                .Include(i => i.Items).ThenInclude(ii => ii.VehiclePart)
                .Include(i => i.Items).ThenInclude(ii => ii.VehiclePartCategory)
                .FirstOrDefaultAsync(i => i.Id == invoiceId);

            if (inv == null) return null;

            return new RepairInvoiceDto
            {
                Id = inv.Id,
                RepairId = inv.RepairId,
                InvoiceDate = inv.InvoiceDate,
                Status = inv.Status,
                TotalAmount = inv.TotalAmount,
                Items = inv.Items.Select(ii => new RepairInvoiceItemDto
                {
                    Id = ii.Id,
                    PartId = ii.VehiclePartId,
                    PartName = ii.VehiclePart != null ? ii.VehiclePart.Name : null,
                    PartCategory = ii.VehiclePartCategory != null ? ii.VehiclePartCategory.Name : null,
                    Description = ii.Description,
                    Quantity = ii.Quantity,
                    UnitPrice = ii.UnitPrice,
                    LineTotal = ii.Quantity * ii.UnitPrice
                }).ToList()
            };
        }

        public async Task<MessageResponse<RepairInvoiceDto>> UpdateRepairInvoiceStatusAsync(long invoiceId, InvoiceStatus newStatus)
        {
            EnsureAdminOrOwner();
            var resp = new MessageResponse<RepairInvoiceDto>();
            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                var inv = await _context.RepairInvoices.FindAsync(invoiceId);
                if (inv == null) { resp.Message = "Invoice not found"; return resp; }

                inv.Status = newStatus;
                inv.ModifiedDate = DateTime.UtcNow;
                inv.ModifiedBy = _auth.UserId;

                if (newStatus == InvoiceStatus.Paid)
                {
                    var repair = await _context.Repairs.FindAsync(inv.RepairId);
                    if (repair != null)
                    {
                        repair.Status = RepairStatus.InProgress;
                        repair.ModifiedDate = DateTime.UtcNow;
                        repair.ModifiedBy = _auth.UserId;
                    }
                }

                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                resp.Success = true;
                resp.Result = await GetRepairInvoiceByIdAsync(invoiceId)!;

                // notify driver (if available)
                var repairWithDriver = await _context.Repairs
                    .Include(r => r.Driver).ThenInclude(d => d.User)
                    .FirstOrDefaultAsync(r => r.Id == inv.RepairId);

                if (repairWithDriver?.Driver != null)
                {
                    var title = newStatus == InvoiceStatus.Paid ? "Repair Invoice Paid" : "Repair Invoice Updated";
                    var msg = newStatus == InvoiceStatus.Paid
                        ? $"Repair invoice #{invoiceId} marked as Paid."
                        : $"Repair invoice #{invoiceId} updated to {newStatus}.";
                    await _notification.CreateAsync(repairWithDriver.Driver.UserId, title, msg, NotificationType.Success, new { invoiceId });
                }
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _logger.LogError(ex, "Error updating repair invoice status {InvoiceId}", invoiceId);
                resp.Message = "Failed to update invoice status.";
            }
            return resp;
        }



        #region Dropdowns

        public async Task<List<SelectListItem>> GetPartCategoriesAsync()
            => await _context.VehiclePartCategories.AsNoTracking()
                .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name })
                .ToListAsync();

        public async Task<List<SelectListItem>> GetPartsByCategoryAsync(int categoryId)
            => await _context.VehicleParts.AsNoTracking()
                .Where(p => p.VehiclePartCategoryId == categoryId)
                .Select(p => new SelectListItem { Value = p.Id.ToString(), Text = p.Name })
                .ToListAsync();

        public List<SelectListItem> GetPriorityTypeOptions() =>
           Enum.GetValues<MaintenancePriority>()
               .Select(e => new SelectListItem
               {
                   Value = ((int)e).ToString(),
                   Text = e.ToString()
               })
               .ToList();

        #endregion

    }

}
