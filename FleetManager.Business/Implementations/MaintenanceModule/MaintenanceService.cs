using FleetManager.Business.Database.Entities.MaintenanceTicket;
using FleetManager.Business.DataObjects.MaintenanceDto;
using FleetManager.Business.Enums;
using FleetManager.Business.Interfaces.MaintenanceModule;
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

namespace FleetManager.Business.Implementations.MaintenanceModule
{
    public class MaintenanceService : IMaintenanceService
    {
        private readonly FleetManagerDbContext _context;
        private readonly IAuthUser _auth;
        private readonly INotificationService _notification;
        private readonly ILogger<MaintenanceService> _logger;

        public MaintenanceService(
            FleetManagerDbContext context,
            IAuthUser authUser,
            ILogger<MaintenanceService> logger,
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
            {
                throw new UnauthorizedAccessException("You do not have permission.");

            }
        }

        #region Tickets

        public async Task<MessageResponse<PaginatedResult<MaintenanceTicketDto>>> QueryTicketsByBranchAsync(int page, int pageSize, long? branchId = null)
        {
            var resp = new MessageResponse<PaginatedResult<MaintenanceTicketDto>>();

            try
            {
                EnsureAdminOrOwner();
                var b = branchId ?? _auth.CompanyBranchId;
                var query = _context.MaintenanceTickets.AsNoTracking()
                    .Include(t => t.Driver).ThenInclude(d => d.User)
                    .Include(t => t.Vehicle)
                    .Include(t => t.Items).ThenInclude(i => i.VehiclePartCategory)
                    .Where(t => t.CompanyBranchId == b)
                    .OrderByDescending(t => t.CreatedDate)
                    .Select(t => new MaintenanceTicketDto
                    {
                        Id = t.Id,
                        DriverId = t.DriverId,
                        DriverName = t.Driver.User.FirstName + " " + t.Driver.User.LastName,
                        VehicleId = t.VehicleId,
                        VehicleDescription = $"{t.Vehicle.VehicleMake.Name} {t.Vehicle.VehicleModel.Name} ({t.Vehicle.PlateNo})",
                        Subject = t.Subject,
                        Notes = t.Notes,
                        Status = t.Status,
                        Priority=t.Priority,
                        AdminNotes = t.AdminNotes,
                        CreatedAt = t.CreatedDate,
                        ResolvedAt = t.ResolvedAt,
                        Items = t.Items.Select(i => new MaintenanceTicketItemDto
                        {
                            Id = i.Id,
                            PartId = i.VehiclePartId,
                            PartName = i.VehiclePart.Name,
                            PartCategoryName = i.VehiclePartCategory.Name,
                            CustomDescription = i.CustomPartDescription,
                            Quantity = i.Quantity,
                            UnitPrice = i.UnitPrice,
                            LineTotal = i.Quantity * i.UnitPrice
                        }).ToList(),
                        Invoice = t.Invoice == null ? null : new InvoiceDto
                        {
                            Id = t.Invoice.Id,
                            TicketId = t.Invoice.MaintenanceTicketId,
                            InvoiceDate = t.Invoice.InvoiceDate,
                            Status = t.Invoice.Status,
                            TotalAmount = t.Invoice.TotalAmount,
                            Items = t.Invoice.Items.Select(ii => new InvoiceItemDto
                            {
                                Id = ii.Id,
                                PartId = ii.VehiclePartId,
                                PartName = ii.VehiclePart.Name,
                                PartCategory = ii.VehiclePartCategory.Name,
                                CustomPartDescription = ii.Description,
                                Quantity = ii.Quantity,
                                UnitPrice = ii.UnitPrice,
                                LineTotal = ii.Quantity * ii.UnitPrice
                            }).ToList()
                        }
                    });
                var paged = await PaginatedResult<MaintenanceTicketDto>.CreateAsync(query, page, pageSize);

                resp.Success = true;
                resp.Result = paged;
            }
            catch (UnauthorizedAccessException uaEx)
            {
                _logger.LogWarning(uaEx, "Permission denied querying tickets by branch");
                resp.Message = uaEx.Message;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error querying tickets by branch");
                resp.Message = "Failed to load tickets.";
            }

            return resp;

        }

        public async Task<MessageResponse<PaginatedResult<MaintenanceTicketDto>>> QueryTicketsByDriverAsync(int page, int pageSize, long? driverId)
        {
            var resp = new MessageResponse<PaginatedResult<MaintenanceTicketDto>>();

            try
            {
                //var dId = driverId ?? _auth.UserId;
                var query = _context.MaintenanceTickets.AsNoTracking()
                    .Include(t => t.Driver).ThenInclude(d => d.User)
                    .Include(t => t.Vehicle)
                    .Include(t => t.Items).ThenInclude(i => i.VehiclePartCategory)
                    .Where(t => t.Driver.Id == driverId)
                    .OrderByDescending(t => t.CreatedDate)
                    .Select(t => new MaintenanceTicketDto
                    {
                        Id = t.Id,
                        DriverId = t.DriverId,
                        DriverName = t.Driver.User.FirstName + " " + t.Driver.User.LastName,
                        VehicleId = t.VehicleId,
                        VehicleDescription = $"{t.Vehicle.VehicleMake.Name} {t.Vehicle.VehicleModel.Name} ({t.Vehicle.PlateNo})",
                        Subject = t.Subject,
                        Notes = t.Notes,
                        Status = t.Status,
                        Priority = t.Priority,
                        AdminNotes = t.AdminNotes,
                        CreatedAt = t.CreatedDate,
                        ResolvedAt = t.ResolvedAt,
                        Items = t.Items.Select(i => new MaintenanceTicketItemDto
                        {
                            Id = i.Id,
                            PartId = i.VehiclePartId,
                            PartName = i.VehiclePart.Name,
                            PartCategoryName = i.VehiclePartCategory.Name,
                            CustomDescription = i.CustomPartDescription,
                            Quantity = i.Quantity,
                            UnitPrice = i.UnitPrice,
                            LineTotal = i.Quantity * i.UnitPrice
                        }).ToList(),
                        Invoice = t.Invoice == null ? null : new InvoiceDto
                        {
                            Id = t.Invoice.Id,
                            TicketId = t.Invoice.MaintenanceTicketId,
                            InvoiceDate = t.Invoice.InvoiceDate,
                            Status = t.Invoice.Status,
                            TotalAmount = t.Invoice.TotalAmount,
                            Items = t.Invoice.Items.Select(ii => new InvoiceItemDto
                            {
                                Id = ii.Id,
                                PartId = ii.VehiclePartId,
                                PartName = ii.VehiclePart.Name,
                                PartCategory = ii.VehiclePartCategory.Name,
                                CustomPartDescription = ii.Description,
                                Quantity = ii.Quantity,
                                UnitPrice = ii.UnitPrice,
                                LineTotal = ii.Quantity * ii.UnitPrice
                            }).ToList()
                        }
                    });
                var paged = await PaginatedResult<MaintenanceTicketDto>.CreateAsync(query, page, pageSize);

                resp.Success = true;
                resp.Result = paged;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error querying tickets by driver");
                resp.Message = "Failed to load tickets.";
            }

            return resp;

        }

        public async Task<MessageResponse<PaginatedResult<MaintenanceTicketDto>>> QueryTicketsByVehicleAsync(int page, int pageSize, long vehicleId)
        {
            var resp = new MessageResponse<PaginatedResult<MaintenanceTicketDto>>();

            try
            {
                EnsureAdminOrOwner();
                var query = _context.MaintenanceTickets.AsNoTracking()
                    .Include(t => t.Driver).ThenInclude(d => d.User)
                    .Include(t => t.Vehicle)
                    .Include(t => t.Items).ThenInclude(i => i.VehiclePartCategory)
                    .Where(t => t.VehicleId == vehicleId)
                    .OrderByDescending(t => t.CreatedDate)
                    .Select(t => new MaintenanceTicketDto
                    {
                        Id = t.Id,
                        DriverId = t.DriverId,
                        DriverName = t.Driver.User.FirstName + " " + t.Driver.User.LastName,
                        VehicleId = t.VehicleId,
                        VehicleDescription = $"{t.Vehicle.VehicleMake.Name} {t.Vehicle.VehicleModel.Name} ({t.Vehicle.PlateNo})",
                        Subject = t.Subject,
                        Notes = t.Notes,
                        Status = t.Status,
                        Priority = t.Priority,
                        AdminNotes = t.AdminNotes,
                        CreatedAt = t.CreatedDate,
                        ResolvedAt = t.ResolvedAt,
                        Items = t.Items.Select(i => new MaintenanceTicketItemDto
                        {
                            Id = i.Id,
                            PartId = i.VehiclePartId,
                            PartName = i.VehiclePart.Name,
                            PartCategoryName = i.VehiclePartCategory.Name,
                            CustomDescription = i.CustomPartDescription,
                            Quantity = i.Quantity,
                            UnitPrice = i.UnitPrice,
                            LineTotal = i.Quantity * i.UnitPrice
                        }).ToList(),
                        Invoice = t.Invoice == null ? null : new InvoiceDto
                        {
                            Id = t.Invoice.Id,
                            TicketId = t.Invoice.MaintenanceTicketId,
                            InvoiceDate = t.Invoice.InvoiceDate,
                            Status = t.Invoice.Status,
                            TotalAmount = t.Invoice.TotalAmount,
                            Items = t.Invoice.Items.Select(ii => new InvoiceItemDto
                            {
                                Id = ii.Id,
                                PartId = ii.VehiclePartId,
                                PartName = ii.VehiclePart.Name,
                                PartCategory = ii.VehiclePartCategory.Name,
                                CustomPartDescription = ii.Description,
                                Quantity = ii.Quantity,
                                UnitPrice = ii.UnitPrice,
                                LineTotal = ii.Quantity * ii.UnitPrice
                            }).ToList()
                        }
                    });
                var paged = await PaginatedResult<MaintenanceTicketDto>.CreateAsync(query, page, pageSize);

                resp.Success = true;
                resp.Result = paged;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error querying tickets by Vehicle");
                resp.Message = "Failed to load tickets.";
            }

            return resp;

        }

        public async Task<MaintenanceTicketDto?> GetTicketByIdAsync(long ticketId)
        {
            var t = await _context.MaintenanceTickets
                .AsNoTracking()
                .Include(t => t.Driver)
                    .ThenInclude(d => d.User)
                .Include(t => t.Vehicle)
                    .ThenInclude(v => v.VehicleMake)
                .Include(t => t.Vehicle)
                    .ThenInclude(v => v.VehicleModel)
                // load both nav‐props off of Items:
                .Include(t => t.Items)
                    .ThenInclude(i => i.VehiclePartCategory)
                .Include(t => t.Items)
                    .ThenInclude(i => i.VehiclePart)
                // load invoice + its items too
                .Include(t => t.Invoice)
                    .ThenInclude(inv => inv.Items)
                        .ThenInclude(ii => ii.VehiclePartCategory)
                .Include(t => t.Invoice)
                    .ThenInclude(inv => inv.Items)
                        .ThenInclude(ii => ii.VehiclePart)
                .FirstOrDefaultAsync(t => t.Id == ticketId);

            if (t == null) return null;


            return new MaintenanceTicketDto
            {
                Id = t.Id,
                DriverId = t.DriverId,
                DriverName = $"{t.Driver.User.FirstName} {t.Driver.User.LastName}",
                VehicleId = t.VehicleId,
                VehicleDescription = $"{t.Vehicle.VehicleMake.Name} {t.Vehicle.VehicleModel.Name} ({t.Vehicle.PlateNo})",
                Subject = t.Subject,
                Notes = t.Notes,
                Status = t.Status,
                Priority = t.Priority,
                AdminNotes = t.AdminNotes,
                CreatedAt = t.CreatedDate,
                ResolvedAt = t.ResolvedAt,
                Items = t.Items.Select(i => new MaintenanceTicketItemDto
                {
                    Id = i.Id,
                    PartId = i.VehiclePartId,
                    PartName = i.VehiclePart.Name,
                    PartCategoryName = i.VehiclePartCategory.Name ?? "",
                    CustomDescription = i.CustomPartDescription ?? "",
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    LineTotal = i.Quantity * i.UnitPrice
                }).ToList(),
                Invoice = t.Invoice == null ? null : new InvoiceDto
                {
                    Id = t.Invoice.Id,
                    TicketId = t.Invoice.MaintenanceTicketId,
                    InvoiceDate = t.Invoice.InvoiceDate,
                    Status = t.Invoice.Status,
                    TotalAmount = t.Invoice.TotalAmount,
                    Items = t.Invoice.Items.Select(ii => new InvoiceItemDto
                    {
                        Id = ii.Id,
                        PartId = ii.VehiclePartId,
                        PartName = ii.VehiclePart.Name ?? "",
                        PartCategory = ii.VehiclePartCategory.Name ?? "",
                        CustomPartDescription = ii.Description ,
                        Quantity = ii.Quantity,
                        UnitPrice = ii.UnitPrice,
                        LineTotal = ii.Quantity * ii.UnitPrice
                    }).ToList()
                }
            };
        }



        public async Task<MessageResponse<MaintenanceTicketDto>> CreateTicketAsync(MaintenanceTicketInputDto input, string createdByUserId)
        {
            var resp = new MessageResponse<MaintenanceTicketDto>();
            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {

                // only drivers
                //var roles = (_auth.Roles ?? "").Split(',').Select(r => r.Trim());
                //if (!roles.Contains("Driver"))
                //    throw new UnauthorizedAccessException("Only drivers can log fines/tolls.");

                // load driver entity to get their name
                var driver = await _context.Drivers
                                           .Include(d => d.User)
                                           .SingleAsync(d => d.UserId == createdByUserId);
                var driverName = $"{driver.User.FirstName} {driver.User.LastName}";

                var branchId = _auth.CompanyBranchId;
                //var driverId = driver.Id;


                var ticket = new MaintenanceTicket
                {
                    DriverId = input.DriverId,
                    VehicleId = input.VehicleId,
                    CompanyBranchId = branchId,
                    Subject = input.Subject,
                    Notes = input.Notes,
                    Status = TicketStatus.Pending,
                    Priority = input.Priority,
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = _auth.UserId
                };
                _context.MaintenanceTickets.Add(ticket);
                await _context.SaveChangesAsync();

                foreach (var item in input.Items)
                {
                    _context.MaintenanceTicketItems.Add(new MaintenanceTicketItem
                    {
                        TicketId = ticket.Id,
                        VehiclePartCategoryId = item.PartCategoryId,
                        VehiclePartId = item.PartId,
                        CustomPartDescription = item.CustomDescription,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice
                    });
                }
                await _context.SaveChangesAsync();

                // Create invoice
                var inv = new Invoice
                {
                    MaintenanceTicketId = ticket.Id,
                    CompanyBranchId = ticket.CompanyBranchId,
                    InvoiceDate = DateTime.UtcNow,
                    Status = InvoiceStatus.Pending,
                    TotalAmount = input.Items.Sum(i => i.Quantity * i.UnitPrice),
                    CreatedBy = _auth.UserId

                };
                _context.Invoices.Add(inv);
                await _context.SaveChangesAsync();

                foreach (var item in input.Items)
                {
                    _context.InvoiceItems.Add(new InvoiceItem
                    {
                        InvoiceId = inv.Id,
                        Description = item.CustomDescription,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        VehiclePartId = item.PartId,
                        VehiclePartCategoryId = item.PartCategoryId

                    });
                }
                await _context.SaveChangesAsync();

                await tx.CommitAsync();

                var dto = await GetTicketByIdAsync(ticket.Id)!;

                resp.Success = true;
                resp.Result = dto;

                // Notify admins of new ticket
                string urgency = input.Priority switch
                {
                    MaintenancePriority.Low => "low priority",
                    MaintenancePriority.Moderate => "moderate priority",
                    MaintenancePriority.High => "high priority",
                    MaintenancePriority.Urgent => "urgent",
                    null => "unspecified",
                    _ => "unspecified"
                };
                var admins = await _context.CompanyAdmins
                    .Where(a => a.CompanyBranchId == ticket.CompanyBranchId && a.IsActive)
                    .Select(a => a.UserId).ToListAsync();
                foreach (var adminId in admins)
                {
                    await _notification.CreateAsync(
                        adminId,
                        "New Maintenance Ticket",
                        $" New {urgency} ticket #{ticket.Id} was created by {resp.Result.DriverName}",
                        NotificationType.Info,
                        new { ticketId = ticket.Id });
                }
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _logger.LogError(ex, "Error creating maintenance ticket");
                resp.Message = "Failed to create ticket.";
            }
            return resp;
        }


        public async Task<MessageResponse<MaintenanceTicketDto>> UpdateTicketStatusAsync(UpdateTicketStatusDto input)
        {
            EnsureAdminOrOwner();
            var resp = new MessageResponse<MaintenanceTicketDto>();
            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                var ticket = await _context.MaintenanceTickets
                    .Include(t => t.Invoice)
                    .Include(t => t.Driver).ThenInclude(d => d.User)
                    .FirstOrDefaultAsync(t => t.Id == input.TicketId);
                if (ticket == null) { resp.Message = "Ticket not found"; return resp; }

                // handle rejection
                if (input.NewStatus == TicketStatus.Rejected && ticket.Invoice != null)
                {
                    ticket.Invoice.Status = InvoiceStatus.Cancelled;
                    ticket.Invoice.ModifiedDate = DateTime.UtcNow;
                    ticket.Invoice.ModifiedBy = _auth.UserId;
                    await _notification.CreateAsync(
                        ticket.Driver.UserId,
                        "Invoice Cancelled",
                        $"Your Invoice #{ticket.Invoice.Id} cancelled due to ticket rejection",
                        NotificationType.Warning,
                        new { invoiceId = ticket.Invoice.Id });
                }

                // update invoice status if provided
                if (input.InvoiceStatus.HasValue && ticket.Invoice != null)
                {
                    ticket.Invoice.Status = input.InvoiceStatus.Value;
                    ticket.Invoice.ModifiedDate = DateTime.UtcNow;
                    ticket.Invoice.ModifiedBy = _auth.UserId;

                    if (input.InvoiceStatus == InvoiceStatus.Paid)
                        ticket.Status = TicketStatus.InProgress;
                }

                // update ticket status
                ticket.Status = input.NewStatus;
                ticket.AdminNotes = input.AdminNotes;
                if (input.NewStatus == TicketStatus.Resolved)
                    ticket.ResolvedAt = DateTime.UtcNow;
                ticket.ModifiedDate = DateTime.UtcNow;
                ticket.ModifiedBy = _auth.UserId;

                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                

                // notify driver
                await _notification.CreateAsync(
                    ticket.Driver.UserId,
                    $"Ticket #{ticket.Id} Updated",
                    $"Your ticket status is now {ticket.Status} and invoice status {ticket.Invoice?.Status}",
                    NotificationType.Info,
                    new { ticketId = ticket.Id });

                resp.Success = true;
                resp.Result = await GetTicketByIdAsync(ticket.Id);
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _logger.LogError(ex, "Error updating ticket status {TicketId}", input.TicketId);
                throw;
            }
            return resp;
        }



        #endregion

        #region Invoices

        public async Task<MessageResponse<PaginatedResult<InvoiceDto>>> QueryInvoicesByBranchAsync(int page, int pageSize, long? branchId = null)
        {
            var resp = new MessageResponse<PaginatedResult<InvoiceDto>>();

            try
            {

                EnsureAdminOrOwner();
                var b = branchId ?? _auth.CompanyBranchId;
                var query = _context.Invoices.AsNoTracking()
                    .Include(i => i.MaintenanceTicket).ThenInclude(t => t.Driver).ThenInclude(d => d.User)
                    .Include(i => i.MaintenanceTicket).ThenInclude(t => t.Vehicle)
                    .Where(i => i.CompanyBranchId == b)
                    .OrderByDescending(i => i.InvoiceDate)
                    .Select(i => new InvoiceDto
                    {
                        Id = i.Id,
                        TicketId = i.MaintenanceTicketId,
                        InvoiceDate = i.InvoiceDate,
                        Status = i.Status,
                        TotalAmount = i.TotalAmount,
                        Items = i.Items.Select(ii => new InvoiceItemDto
                        {
                            Id = ii.Id,
                            PartId = ii.VehiclePartId,
                            PartName = ii.VehiclePart.Name,
                            PartCategory = ii.VehiclePartCategory.Name,
                            CustomPartDescription = ii.Description,
                            Quantity = ii.Quantity,
                            UnitPrice = ii.UnitPrice,
                            LineTotal = ii.Quantity * ii.UnitPrice
                        }).ToList()
                    });

                var paged = await PaginatedResult<InvoiceDto>.CreateAsync(query, page, pageSize);

                resp.Success = true;
                resp.Result = paged;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error querying invoices by driver");
                resp.Message = "Failed to load invoices.";
            }

            return resp;
        }

        public async Task<MessageResponse<PaginatedResult<InvoiceDto>>> QueryInvoicesByDriverAsync(int page, int pageSize, long? driverId)
        {
            var resp = new MessageResponse<PaginatedResult<InvoiceDto>>();

            try
            {
                //var d = driverId ?? _auth.UserId;
                var query = _context.Invoices.AsNoTracking()
               .Include(i => i.MaintenanceTicket).ThenInclude(t => t.Driver).ThenInclude(dv => dv.User)
               .Include(i => i.MaintenanceTicket).ThenInclude(t => t.Vehicle)
               .Where(i => i.MaintenanceTicket.Driver.Id == driverId)
               .OrderByDescending(i => i.InvoiceDate)
               .Select(i => new InvoiceDto
               {
                   Id = i.Id,
                   TicketId = i.MaintenanceTicketId,
                   InvoiceDate = i.InvoiceDate,
                   Status = i.Status,
                   TotalAmount = i.TotalAmount,
                   Items = i.Items.Select(ii => new InvoiceItemDto
                   {
                       Id = ii.Id,
                       PartId = ii.VehiclePartId,
                       PartName = ii.VehiclePart.Name,
                       PartCategory = ii.VehiclePartCategory.Name,
                       CustomPartDescription = ii.Description,
                       Quantity = ii.Quantity,
                       UnitPrice = ii.UnitPrice,
                       LineTotal = ii.Quantity * ii.UnitPrice
                   }).ToList()
               });
                var paged = await PaginatedResult<InvoiceDto>.CreateAsync(query, page, pageSize);

                resp.Success = true;
                resp.Result = paged;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error querying invoices by driver");
                resp.Message = "Failed to load invoices.";
            }

            return resp;

            
        }

        public async Task<MessageResponse<PaginatedResult<InvoiceDto>>> QueryInvoicesByVehicleAsync(int page, int pageSize, long vehicleId)
        {
            var resp = new MessageResponse<PaginatedResult<InvoiceDto>>();

            try
            {
                EnsureAdminOrOwner();
                var query = _context.Invoices.AsNoTracking()
                    .Include(i => i.MaintenanceTicket).ThenInclude(t => t.Driver).ThenInclude(d => d.User)
                    .Include(i => i.MaintenanceTicket).ThenInclude(t => t.Vehicle)
                    .Where(i => i.MaintenanceTicket.VehicleId == vehicleId)
                    .OrderByDescending(i => i.InvoiceDate)
                    .Select(i => new InvoiceDto
                    {
                        Id = i.Id,
                        TicketId = i.MaintenanceTicketId,
                        InvoiceDate = i.InvoiceDate,
                        Status = i.Status,
                        TotalAmount = i.TotalAmount,
                        Items = i.Items.Select(ii => new InvoiceItemDto
                        {
                            Id = ii.Id,
                            PartId = ii.VehiclePartId,
                            PartName = ii.VehiclePart.Name,
                            PartCategory = ii.VehiclePartCategory.Name,
                            CustomPartDescription = ii.Description,
                            Quantity = ii.Quantity,
                            UnitPrice = ii.UnitPrice,
                            LineTotal = ii.Quantity * ii.UnitPrice
                        }).ToList()
                    });
                var paged = await PaginatedResult<InvoiceDto>.CreateAsync(query, page, pageSize);
                resp.Success = true;
                resp.Result = paged;

            }
            catch (Exception)
            {

                throw;
            }
            return resp;

        }

        public async Task<InvoiceDto?> GetInvoiceByIdAsync(long invoiceId)
        {
            var inv = await _context.Invoices.AsNoTracking()
                .Include(i => i.MaintenanceTicket).ThenInclude(t => t.Driver).ThenInclude(d => d.User)
                .Include(i => i.MaintenanceTicket).ThenInclude(t => t.Vehicle)
                .FirstOrDefaultAsync(i => i.Id == invoiceId);
            if (inv == null) return null;
            //if (!_auth.Roles.Split(',').Any(r => new[] { "Super Admin", "Company Owner", "Company Admin" }.Contains(r.Trim()))
            //    && ent.Ticket.DriverId != _auth.UserId)
            //    throw new UnauthorizedAccessException();
            return new InvoiceDto
            {
                Id = inv.Id,
                TicketId = inv.MaintenanceTicketId,
                InvoiceDate = inv.InvoiceDate,
                Status = inv.Status,
                TotalAmount = inv.TotalAmount,
                Items = inv.Items.Select(ii => new InvoiceItemDto
                {
                    Id = ii.Id,
                    PartId = ii.VehiclePartId,
                    PartName = ii.VehiclePart.Name,
                    PartCategory = ii.VehiclePartCategory.Name,
                    CustomPartDescription = ii.Description,
                    Quantity = ii.Quantity,
                    UnitPrice = ii.UnitPrice,
                    LineTotal = ii.Quantity * ii.UnitPrice
                }).ToList()
            };
        }

        public async Task<MessageResponse<InvoiceDto>> UpdateInvoiceStatusAsync(long invoiceId, InvoiceStatus newStatus)
        {
            EnsureAdminOrOwner();
            var resp = new MessageResponse<InvoiceDto>();
            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                var inv = await _context.Invoices.FindAsync(invoiceId);
                if (inv == null) { resp.Message = "Invoice not found"; return resp; }

                inv.Status = newStatus;
                inv.ModifiedDate = DateTime.UtcNow;
                inv.ModifiedBy = _auth.UserId;

                // If paid, move ticket to InProgress
                if (newStatus == InvoiceStatus.Paid)
                {
                    var ticket = await _context.MaintenanceTickets.FindAsync(inv.MaintenanceTicketId);
                    if (ticket != null)
                    {
                        ticket.Status = TicketStatus.InProgress;
                        ticket.ModifiedDate = DateTime.UtcNow;
                        ticket.ModifiedBy = _auth.UserId;
                    }
                }

                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                resp.Success = true;
                resp.Result = await GetInvoiceByIdAsync(invoiceId)!;

                // Notify driver
                var title = newStatus == InvoiceStatus.Paid ? "Invoice Paid" : "Invoice Cancelled";
                var message = newStatus == InvoiceStatus.Paid
                    ? $"Your invoice # {invoiceId} has been marked Paid and ticket moved InProgress."
                    : $"Your invoice # {invoiceId} has been Cancelled.";
                await _notification.CreateAsync(
                    inv.MaintenanceTicket.Driver.UserId,
                    title,
                    message,
                    NotificationType.Success,
                    new { invoiceId });
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _logger.LogError(ex, "Error updating invoice status {InvoiceId}", invoiceId);
                resp.Message = "Failed to update invoice status.";
            }
            return resp;
        }

        #endregion

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


//public async Task<MessageResponse<MaintenanceTicketDto>> UpdateTicketStatusAsync(long ticketId, TicketStatus newStatus)
//{
//    EnsureAdminOrOwner();
//    var resp = new MessageResponse<MaintenanceTicketDto>();
//    using var tx = await _context.Database.BeginTransactionAsync();
//    try
//    {
//        var ticket = await _context.MaintenanceTickets.FindAsync(ticketId);
//        if (ticket == null) { resp.Message = "Ticket not found"; return resp; }

//        // If rejecting, cancel invoice
//        if (newStatus == TicketStatus.Rejected)
//        {
//            var inv = await _context.Invoices.FirstOrDefaultAsync(i => i.TicketId == ticketId);
//            if (inv != null)
//            {
//                inv.Status = InvoiceStatus.Cancelled;
//                inv.ModifiedDate = DateTime.UtcNow;
//                inv.ModifiedBy = _auth.UserId;
//                await _notification.CreateAsync(
//                    ticket.Driver.UserId,
//                    "Invoice Cancelled",
//                    $"Your invoice #{inv.Id} has been cancelled due to ticket rejection.",
//                    NotificationType.Warning,
//                    new { invoiceId = inv.Id });
//            }
//        }

//        ticket.Status = newStatus;
//        if (newStatus == TicketStatus.Resolved)
//            ticket.ResolvedAt = DateTime.UtcNow;
//        ticket.ModifiedDate = DateTime.UtcNow;
//        ticket.ModifiedBy = _auth.UserId;
//        await _context.SaveChangesAsync();

//        await tx.CommitAsync();

//        resp.Success = true;
//        resp.Result = await GetTicketByIdAsync(ticketId)!;

//        // Notify driver
//        await _notification.CreateAsync(
//            ticket.Driver.UserId,
//            "Ticket Status Updated",
//            $"Your ticket #{ticketId} status is now {newStatus}.",
//            NotificationType.Info,
//            new { ticketId });
//    }
//    catch (Exception ex)
//    {
//        await tx.RollbackAsync();
//        _logger.LogError(ex, "Error updating ticket status {TicketId}", ticketId);
//        resp.Message = "Failed to update status.";
//    }
//    return resp;
//}
