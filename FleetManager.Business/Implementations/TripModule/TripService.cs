using FleetManager.Business.Database.Entities;
using FleetManager.Business.DataObjects.TripsDto;
using FleetManager.Business.Enums;
using FleetManager.Business.Interfaces.TripModule;
using FleetManager.Business.Interfaces.UserModule;
using FleetManager.Business.UtilityModels;
using FleetManager.Business.ViewModels.TripsViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Implementations.TripModule
{
    public class TripService : ITripService
    {
        private readonly FleetManagerDbContext _context;
        private readonly IAuthUser _authUser;
        private readonly ILogger<TripService> _logger;

        public TripService(FleetManagerDbContext context, IAuthUser authUser, ILogger<TripService> logger)
        {
            _context = context;
            _authUser = authUser;
            _logger = logger;
        }


        #region CRUD Operations

        public async Task<MessageResponse<TripDto>> CreateTripAsync(CreateTripDto dto)
        {
            try
            {
                if (!_authUser.CompanyBranchId.HasValue || !_authUser.CompanyId.HasValue)
                {
                    return new MessageResponse<TripDto> { Success = false, Message = "Invalid user context. Missing branch or company." };
                }

                var now = DateTime.UtcNow;

                // Date validation
                if (dto.ScheduledEndDate <= dto.ScheduledStartDate)
                {
                    return new MessageResponse<TripDto> { Success = false, Message = "Scheduled end date must be after start date" };
                }

                // Optional: reject start in the past depending on business rules
                // if (dto.ScheduledStartDate < now) { ... }

                // Validate vehicle exists and belongs to branch
                var vehicle = await _context.Vehicles
                    .AsNoTracking()
                    .FirstOrDefaultAsync(v => v.Id == dto.VehicleId &&
                                             v.CompanyBranchId == _authUser.CompanyBranchId &&
                                             v.IsActive);

                if (vehicle == null)
                {
                    return new MessageResponse<TripDto> { Success = false, Message = "Vehicle not found or not available in your branch" };
                }

                // Validate driver if provided
                if (dto.DriverId.HasValue)
                {
                    var driver = await _context.Drivers
                        .AsNoTracking()
                        .FirstOrDefaultAsync(d => d.Id == dto.DriverId &&
                                                  d.CompanyBranchId == _authUser.CompanyBranchId &&
                                                  d.IsActive);

                    if (driver == null)
                    {
                        return new MessageResponse<TripDto> { Success = false, Message = "Driver not found or not available in your branch" };
                    }

                    // Check if driver license is valid (use UTC date)
                    if (driver.LicenseExpiryDate.HasValue && driver.LicenseExpiryDate.Value.Date < now.Date)
                    {
                        return new MessageResponse<TripDto> { Success = false, Message = "Driver's license has expired" };
                    }
                }

                // Check availability
                var availabilityCheck = await ValidateTripAvailabilityAsync(
                    dto.VehicleId,
                    dto.DriverId,
                    dto.ScheduledStartDate,
                    dto.ScheduledEndDate);

                if (!availabilityCheck.Success)
                {
                    return new MessageResponse<TripDto> { Success = false, Message = availabilityCheck.Message };
                }

                // Generate trip number (will throw if it cannot create unique number)
                var tripNumber = await GenerateTripNumberAsync();

                // Create trip entity
                var trip = new Trip
                {
                    TripNumber = tripNumber,
                    CompanyBranchId = _authUser.CompanyBranchId.Value,
                    CompanyId = _authUser.CompanyId.Value,
                    VehicleId = dto.VehicleId,
                    DriverId = dto.DriverId,
                    Origin = dto.Origin,
                    Destination = dto.Destination,
                    Purpose = dto.Purpose,
                    Description = dto.Description,
                    ScheduledStartDate = dto.ScheduledStartDate,
                    ScheduledEndDate = dto.ScheduledEndDate,
                    EstimatedDistance = dto.EstimatedDistance,
                    EstimatedFuelCost = dto.EstimatedFuelCost,
                    Priority = dto.Priority,
                    RequiresApproval = dto.RequiresApproval,
                    Status = dto.RequiresApproval ? TripStatus.PendingApproval : TripStatus.Scheduled,
                    Notes = dto.Notes,
                    IsActive = true,
                    CreatedDate = now,
                    CreatedBy = _authUser.UserId
                };

                // If driver is assigned at creation
                if (dto.DriverId.HasValue)
                {
                    trip.AssignedBy = _authUser.UserId;
                    trip.AssignedDate = now;
                    trip.Status = dto.RequiresApproval ? TripStatus.PendingApproval : TripStatus.Assigned;
                }

                // Use transaction to ensure safe insert
                using var tx = await _context.Database.BeginTransactionAsync();
                try
                {
                    _context.Trips.Add(trip);
                    await _context.SaveChangesAsync();
                    await tx.CommitAsync();
                }
                catch
                {
                    await tx.RollbackAsync();
                    throw;
                }

                _logger.LogInformation("Trip created", new { TripNumber = tripNumber, UserId = _authUser.UserId });

                var result = await GetTripByIdAsync(trip.Id);
                return new MessageResponse<TripDto>
                {
                    Success = true,
                    Message = "Trip created successfully",
                    Result = result.Result
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating trip");
                return new MessageResponse<TripDto> { Success = false, Message = "An error occurred while creating the trip" };
            }
        }

        public async Task<MessageResponse<TripDto>> UpdateTripAsync(UpdateTripDto dto)
        {
            try
            {
                if (!_authUser.CompanyBranchId.HasValue) return new MessageResponse<TripDto> { Success = false, Message = "Invalid user context. Missing branch." };

                var now = DateTime.UtcNow;

                var trip = await _context.Trips
                    .FirstOrDefaultAsync(t => t.Id == dto.Id &&
                                              t.CompanyBranchId == _authUser.CompanyBranchId &&
                                              t.IsActive);

                if (trip == null) return new MessageResponse<TripDto> { Success = false, Message = "Trip not found" };

                // Don't allow updates to trips in certain statuses
                if (trip.Status == TripStatus.InProgress || trip.Status == TripStatus.Completed)
                {
                    return new MessageResponse<TripDto> { Success = false, Message = $"Cannot update trip with status '{trip.Status}'" };
                }

                // Validate dates
                if (dto.ScheduledEndDate <= dto.ScheduledStartDate)
                {
                    return new MessageResponse<TripDto> { Success = false, Message = "Scheduled end date must be after start date" };
                }

                // Validate vehicle
                var vehicle = await _context.Vehicles
                    .AsNoTracking()
                    .FirstOrDefaultAsync(v => v.Id == dto.VehicleId &&
                                              v.CompanyBranchId == _authUser.CompanyBranchId &&
                                              v.IsActive);

                if (vehicle == null) return new MessageResponse<TripDto> { Success = false, Message = "Vehicle not found" };

                // Validate driver if provided
                if (dto.DriverId.HasValue)
                {
                    var driver = await _context.Drivers
                        .AsNoTracking()
                        .FirstOrDefaultAsync(d => d.Id == dto.DriverId &&
                                                  d.CompanyBranchId == _authUser.CompanyBranchId &&
                                                  d.IsActive);

                    if (driver == null) return new MessageResponse<TripDto> { Success = false, Message = "Driver not found" };
                }

                // Check availability (exclude current trip)
                var availabilityCheck = await ValidateTripAvailabilityAsync(
                    dto.VehicleId,
                    dto.DriverId,
                    dto.ScheduledStartDate,
                    dto.ScheduledEndDate,
                    dto.Id);

                if (!availabilityCheck.Success) return new MessageResponse<TripDto> { Success = false, Message = availabilityCheck.Message };

                // Update trip
                trip.VehicleId = dto.VehicleId;
                trip.DriverId = dto.DriverId;
                trip.Origin = dto.Origin;
                trip.Destination = dto.Destination;
                trip.Purpose = dto.Purpose;
                trip.Description = dto.Description;
                trip.ScheduledStartDate = dto.ScheduledStartDate;
                trip.ScheduledEndDate = dto.ScheduledEndDate;
                trip.EstimatedDistance = dto.EstimatedDistance;
                trip.EstimatedFuelCost = dto.EstimatedFuelCost;
                trip.Priority = dto.Priority;
                trip.RequiresApproval = dto.RequiresApproval;
                trip.Notes = dto.Notes;
                trip.ModifiedDate = now;
                trip.ModifiedBy = _authUser.UserId;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Trip updated", new { TripNumber = trip.TripNumber, UserId = _authUser.UserId });

                var result = await GetTripByIdAsync(trip.Id);
                return new MessageResponse<TripDto> { Success = true, Message = "Trip updated successfully", Result = result.Result };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating trip");
                return new MessageResponse<TripDto> { Success = false, Message = "An error occurred while updating the trip" };
            }
        }

        public async Task<MessageResponse<TripDto>> GetTripByIdAsync(long id)
        {
            try
            {
                if (!_authUser.CompanyBranchId.HasValue) return new MessageResponse<TripDto> { Success = false, Message = "Invalid user context. Missing branch." };

                var trip = await _context.Trips
                    .AsNoTracking()
                    .Include(t => t.Vehicle)
                        .ThenInclude(v => v.VehicleMake)
                    .Include(t => t.Vehicle)
                        .ThenInclude(v => v.VehicleModel)
                    .Include(t => t.Driver)
                        .ThenInclude(d => d.User)
                    .FirstOrDefaultAsync(t => t.Id == id &&
                                              t.CompanyBranchId == _authUser.CompanyBranchId &&
                                              t.IsActive);

                if (trip == null) return new MessageResponse<TripDto> { Success = false, Message = "Trip not found" };

                var tripDto = MapTripToDto(trip);

                return new MessageResponse<TripDto> { Success = true, Result = tripDto };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving trip");
                return new MessageResponse<TripDto> { Success = false, Message = "An error occurred while retrieving the trip" };
            }
        }

        public async Task<MessageResponse<PaginatedResult<TripListDto>>> GetTripsAsync(TripFilterDto filter)
        {
            try
            {
                if (!_authUser.CompanyBranchId.HasValue) return new MessageResponse<PaginatedResult<TripListDto>> { Success = false, Message = "Invalid user context. Missing branch." };

                var query = _context.Trips
                    .AsNoTracking()
                    .Include(t => t.Vehicle)
                    .Include(t => t.Driver)
                        .ThenInclude(d => d.User)
                    .Where(t => t.CompanyBranchId == _authUser.CompanyBranchId && t.IsActive);

                // Apply filters
                if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
                {
                    var searchTerm = filter.SearchTerm.ToLower();
                    query = query.Where(t =>
                        (t.TripNumber ?? string.Empty).ToLower().Contains(searchTerm) ||
                        (t.Origin ?? string.Empty).ToLower().Contains(searchTerm) ||
                        (t.Destination ?? string.Empty).ToLower().Contains(searchTerm) ||
                        (t.Purpose ?? string.Empty).ToLower().Contains(searchTerm) ||
                        (t.Vehicle != null && (t.Vehicle.PlateNo ?? string.Empty).ToLower().Contains(searchTerm)));
                }

                if (filter.Status.HasValue) query = query.Where(t => t.Status == filter.Status.Value);
                if (filter.Priority.HasValue) query = query.Where(t => t.Priority == filter.Priority.Value);
                if (filter.DriverId.HasValue) query = query.Where(t => t.DriverId == filter.DriverId.Value);
                if (filter.VehicleId.HasValue) query = query.Where(t => t.VehicleId == filter.VehicleId.Value);
                if (filter.StartDate.HasValue) query = query.Where(t => t.ScheduledStartDate >= filter.StartDate.Value);
                if (filter.EndDate.HasValue) query = query.Where(t => t.ScheduledEndDate <= filter.EndDate.Value);

                query = query.OrderByDescending(t => t.ScheduledStartDate);

                // Get total count (for pagination)
                var totalCount = await query.CountAsync();

                var trips = await query
                    .Skip((filter.Page - 1) * filter.PageSize)
                    .Take(filter.PageSize)
                    .Select(t => new TripListDto
                    {
                        Id = t.Id,
                        TripNumber = t.TripNumber,
                        VehiclePlateNo = t.Vehicle != null ? t.Vehicle.PlateNo : null,
                        DriverName = t.Driver != null && t.Driver.User != null ? t.Driver.User.FirstName + " " + t.Driver.User.LastName : null,
                        Origin = t.Origin,
                        Destination = t.Destination,
                        ScheduledStartDate = t.ScheduledStartDate,
                        ScheduledEndDate = t.ScheduledEndDate,
                        Status = t.Status,
                        StatusDisplay = t.Status.ToString(),
                        Priority = t.Priority,
                        PriorityDisplay = t.Priority.ToString(),
                        CreatedDate = t.CreatedDate
                    })
                    .ToListAsync();

                var result = new PaginatedResult<TripListDto> { Items = trips, Page = filter.Page, PageSize = filter.PageSize, TotalCount = totalCount };

                return new MessageResponse<PaginatedResult<TripListDto>> { Success = true, Result = result };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving trips");
                return new MessageResponse<PaginatedResult<TripListDto>> { Success = false, Message = "An error occurred while retrieving trips" };
            }
        }

        public async Task<MessageResponse> DeleteTripAsync(long id)
        {
            try
            {
                if (!_authUser.CompanyBranchId.HasValue) return new MessageResponse { Success = false, Message = "Invalid user context. Missing branch." };

                var trip = await _context.Trips
                    .FirstOrDefaultAsync(t => t.Id == id &&
                                              t.CompanyBranchId == _authUser.CompanyBranchId &&
                                              t.IsActive);

                if (trip == null) return new MessageResponse { Success = false, Message = "Trip not found" };

                // Don't allow deletion of trips in certain statuses
                if (trip.Status == TripStatus.InProgress || trip.Status == TripStatus.Completed)
                {
                    return new MessageResponse { Success = false, Message = $"Cannot delete trip with status '{trip.Status}'" };
                }

                // Soft delete
                trip.IsActive = false;
                trip.ModifiedDate = DateTime.UtcNow;
                trip.ModifiedBy = _authUser.UserId;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Trip deleted", new { TripNumber = trip.TripNumber, UserId = _authUser.UserId });

                return new MessageResponse { Success = true, Message = "Trip deleted successfully" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting trip");
                return new MessageResponse { Success = false, Message = "An error occurred while deleting the trip" };
            }
        }

        #endregion

        #region Trip Assignment & Management

        public async Task<MessageResponse<TripDto>> AssignTripToDriverAsync(AssignTripDto dto)
        {
            try
            {
                if (!_authUser.CompanyBranchId.HasValue) return new MessageResponse<TripDto> { Success = false, Message = "Invalid user context. Missing branch." };

                var now = DateTime.UtcNow;

                var trip = await _context.Trips
                    .Include(t => t.Vehicle)
                    .FirstOrDefaultAsync(t => t.Id == dto.TripId &&
                                              t.CompanyBranchId == _authUser.CompanyBranchId &&
                                              t.IsActive);

                if (trip == null) return new MessageResponse<TripDto> { Success = false, Message = "Trip not found" };

                // Check if trip can be assigned
                if (trip.Status != TripStatus.Scheduled && trip.Status != TripStatus.Approved)
                {
                    return new MessageResponse<TripDto> { Success = false, Message = $"Cannot assign trip with status '{trip.Status}'" };
                }

                // Validate driver
                var driver = await _context.Drivers
                    .Include(d => d.User)
                    .FirstOrDefaultAsync(d => d.Id == dto.DriverId &&
                                              d.CompanyBranchId == _authUser.CompanyBranchId &&
                                              d.IsActive);

                if (driver == null) return new MessageResponse<TripDto> { Success = false, Message = "Driver not found or not available in your branch" };

                // Check driver status
                if (driver.EmploymentStatus == EmploymentStatus.Inactive)
                {
                    return new MessageResponse<TripDto> { Success = false, Message = "Driver is not active" };
                }

                // Check if driver license is valid
                if (driver.LicenseExpiryDate.HasValue && driver.LicenseExpiryDate.Value.Date < now.Date)
                {
                    return new MessageResponse<TripDto> { Success = false, Message = "Driver's license has expired" };
                }

                // Check driver availability for the trip period using centralized overlap logic
                var hasConflict = await _context.Trips
                    .AsNoTracking()
                    .AnyAsync(t => t.DriverId == dto.DriverId &&
                                   t.Id != dto.TripId &&
                                   t.IsActive &&
                                   (t.Status == TripStatus.Scheduled || t.Status == TripStatus.Assigned || t.Status == TripStatus.InProgress) &&
                                   (t.ScheduledStartDate < trip.ScheduledEndDate && t.ScheduledEndDate > trip.ScheduledStartDate));

                if (hasConflict)
                {
                    return new MessageResponse<TripDto> { Success = false, Message = "Driver is already assigned to another trip during this period" };
                }

                // Assign driver to trip inside a transaction
                using var tx = await _context.Database.BeginTransactionAsync();
                try
                {
                    trip.DriverId = dto.DriverId;
                    trip.AssignedBy = _authUser.UserId;
                    trip.AssignedDate = now;
                    trip.Status = TripStatus.Assigned;
                    trip.ModifiedDate = now;
                    trip.ModifiedBy = _authUser.UserId;

                    if (!string.IsNullOrWhiteSpace(dto.Notes))
                    {
                        trip.Notes = string.IsNullOrWhiteSpace(trip.Notes) ? dto.Notes : $"{trip.Notes}\n\nAssignment Notes: {dto.Notes}";
                    }

                    await _context.SaveChangesAsync();
                    await tx.CommitAsync();
                }
                catch
                {
                    await tx.RollbackAsync();
                    throw;
                }

                _logger.LogInformation("Trip assigned", new { TripNumber = trip.TripNumber, DriverId = dto.DriverId, UserId = _authUser.UserId });

                var result = await GetTripByIdAsync(trip.Id);
                return new MessageResponse<TripDto> { Success = true, Message = $"Trip successfully assigned to {driver.User?.FirstName} {driver.User?.LastName}", Result = result.Result };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning trip to driver");
                return new MessageResponse<TripDto> { Success = false, Message = "An error occurred while assigning the trip" };
            }
        }

        public async Task<MessageResponse<TripDto>> UnassignTripAsync(long tripId)
        {
            try
            {
                if (!_authUser.CompanyBranchId.HasValue) return new MessageResponse<TripDto> { Success = false, Message = "Invalid user context. Missing branch." };

                var now = DateTime.UtcNow;

                var trip = await _context.Trips
                    .FirstOrDefaultAsync(t => t.Id == tripId &&
                                              t.CompanyBranchId == _authUser.CompanyBranchId &&
                                              t.IsActive);

                if (trip == null) return new MessageResponse<TripDto> { Success = false, Message = "Trip not found" };

                // Check if trip can be unassigned
                if (trip.Status != TripStatus.Assigned && trip.Status != TripStatus.Scheduled)
                {
                    return new MessageResponse<TripDto> { Success = false, Message = $"Cannot unassign trip with status '{trip.Status}'" };
                }

                if (!trip.DriverId.HasValue) return new MessageResponse<TripDto> { Success = false, Message = "Trip is not assigned to any driver" };

                // Unassign inside transaction
                using var tx = await _context.Database.BeginTransactionAsync();
                try
                {
                    trip.DriverId = null;
                    trip.AssignedBy = null;
                    trip.AssignedDate = null;
                    trip.Status = trip.RequiresApproval && trip.IsApproved == true ? TripStatus.Approved : TripStatus.Scheduled;
                    trip.ModifiedDate = now;
                    trip.ModifiedBy = _authUser.UserId;

                    await _context.SaveChangesAsync();
                    await tx.CommitAsync();
                }
                catch
                {
                    await tx.RollbackAsync();
                    throw;
                }

                _logger.LogInformation("Trip unassigned", new { TripNumber = trip.TripNumber, UserId = _authUser.UserId });

                var result = await GetTripByIdAsync(trip.Id);
                return new MessageResponse<TripDto> { Success = true, Message = "Driver unassigned from trip successfully", Result = result.Result };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unassigning trip");
                return new MessageResponse<TripDto> { Success = false, Message = "An error occurred while unassigning the trip" };
            }
        }

        public async Task<MessageResponse<TripDto>> StartTripAsync(StartTripDto dto)
        {
            try
            {
                if (!_authUser.CompanyBranchId.HasValue) return new MessageResponse<TripDto> { Success = false, Message = "Invalid user context. Missing branch." };

                var now = DateTime.UtcNow;

                var trip = await _context.Trips
                    .Include(t => t.Vehicle)
                    .Include(t => t.Driver)
                    .FirstOrDefaultAsync(t => t.Id == dto.TripId &&
                                              t.CompanyBranchId == _authUser.CompanyBranchId &&
                                              t.IsActive);

                if (trip == null) return new MessageResponse<TripDto> { Success = false, Message = "Trip not found" };

                // Validate trip status
                if (trip.Status != TripStatus.Assigned)
                {
                    return new MessageResponse<TripDto> { Success = false, Message = $"Cannot start trip with status '{trip.Status}'. Trip must be assigned first." };
                }

                if (!trip.DriverId.HasValue) return new MessageResponse<TripDto> { Success = false, Message = "Trip must be assigned to a driver before starting" };

                // Validate odometer reading
                if (trip.Vehicle != null && trip.Vehicle.Mileage.HasValue && dto.StartOdometer < trip.Vehicle.Mileage.Value)
                {
                    return new MessageResponse<TripDto> { Success = false, Message = $"Start odometer reading ({dto.StartOdometer} km) cannot be less than vehicle's current mileage ({trip.Vehicle.Mileage.Value} km)" };
                }

                // Start trip inside a transaction to update trip, vehicle and create checkpoint atomically
                using var tx = await _context.Database.BeginTransactionAsync();
                try
                {
                    trip.ActualStartDate = now;
                    trip.StartOdometer = dto.StartOdometer;
                    trip.Status = TripStatus.InProgress;
                    trip.ModifiedDate = now;
                    trip.ModifiedBy = _authUser.UserId;

                    // Update vehicle mileage
                    if (trip.Vehicle != null) trip.Vehicle.Mileage = dto.StartOdometer;

                    // Update driver shift status
                    if (trip.Driver != null)
                    {
                        trip.Driver.ShiftStatus = ShiftStatus.OnDuty;
                        trip.Driver.LastSeen = now;
                    }

                    // Create start checkpoint
                    var checkpoint = new TripCheckpoint
                    {
                        TripId = trip.Id,
                        Location = trip.Origin,
                        Description = "Trip started",
                        CheckpointTime = now,
                        CheckpointType = CheckpointType.Start,
                        Latitude = dto.Latitude,
                        Longitude = dto.Longitude,
                        Notes = dto.Notes,
                        IsActive = true,
                        CreatedDate = now,
                        CreatedBy = _authUser.UserId
                    };

                    _context.TripCheckpoints.Add(checkpoint);
                    await _context.SaveChangesAsync();
                    await tx.CommitAsync();
                }
                catch
                {
                    await tx.RollbackAsync();
                    throw;
                }

                _logger.LogInformation("Trip started", new { TripNumber = trip.TripNumber, UserId = _authUser.UserId });

                var result = await GetTripByIdAsync(trip.Id);
                return new MessageResponse<TripDto> { Success = true, Message = "Trip started successfully", Result = result.Result };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting trip");
                return new MessageResponse<TripDto> { Success = false, Message = "An error occurred while starting the trip" };
            }
        }

        public async Task<MessageResponse<TripDto>> CompleteTripAsync(CompleteTripDto dto)
        {
            try
            {
                if (!_authUser.CompanyBranchId.HasValue) return new MessageResponse<TripDto> { Success = false, Message = "Invalid user context. Missing branch." };

                var now = DateTime.UtcNow;

                var trip = await _context.Trips
                    .Include(t => t.Vehicle)
                    .Include(t => t.Driver)
                    .FirstOrDefaultAsync(t => t.Id == dto.TripId &&
                                              t.CompanyBranchId == _authUser.CompanyBranchId &&
                                              t.IsActive);

                if (trip == null) return new MessageResponse<TripDto> { Success = false, Message = "Trip not found" };

                // Validate trip status
                if (trip.Status != TripStatus.InProgress)
                {
                    return new MessageResponse<TripDto> { Success = false, Message = $"Cannot complete trip with status '{trip.Status}'. Trip must be in progress." };
                }

                // Validate odometer reading
                if (!trip.StartOdometer.HasValue) return new MessageResponse<TripDto> { Success = false, Message = "Trip does not have a start odometer reading" };

                if (dto.EndOdometer <= trip.StartOdometer.Value) return new MessageResponse<TripDto> { Success = false, Message = $"End odometer reading ({dto.EndOdometer} km) must be greater than start odometer ({trip.StartOdometer.Value} km)" };

                // Complete trip inside transaction
                using var tx = await _context.Database.BeginTransactionAsync();
                try
                {
                    trip.ActualEndDate = now;
                    trip.EndOdometer = dto.EndOdometer;
                    trip.ActualDistance = dto.EndOdometer - trip.StartOdometer.Value;
                    trip.ActualFuelCost = dto.ActualFuelCost;
                    trip.Status = TripStatus.Completed;
                    trip.ModifiedDate = now;
                    trip.ModifiedBy = _authUser.UserId;

                    // Update vehicle mileage
                    if (trip.Vehicle != null) trip.Vehicle.Mileage = dto.EndOdometer;

                    // Update driver shift status
                    if (trip.Driver != null)
                    {
                        trip.Driver.ShiftStatus = ShiftStatus.Available;
                        trip.Driver.LastSeen = now;
                    }

                    // Create end checkpoint
                    var checkpoint = new TripCheckpoint
                    {
                        TripId = trip.Id,
                        Location = trip.Destination,
                        Description = "Trip completed",
                        CheckpointTime = now,
                        CheckpointType = CheckpointType.End,
                        Latitude = dto.Latitude,
                        Longitude = dto.Longitude,
                        Notes = dto.Notes,
                        IsActive = true,
                        CreatedDate = now,
                        CreatedBy = _authUser.UserId
                    };

                    _context.TripCheckpoints.Add(checkpoint);
                    await _context.SaveChangesAsync();
                    await tx.CommitAsync();
                }
                catch
                {
                    await tx.RollbackAsync();
                    throw;
                }

                _logger.LogInformation("Trip completed", new { TripNumber = trip.TripNumber, Distance = trip.ActualDistance, UserId = _authUser.UserId });

                var result = await GetTripByIdAsync(trip.Id);
                return new MessageResponse<TripDto> { Success = true, Message = $"Trip completed successfully. Distance covered: {trip.ActualDistance} km", Result = result.Result };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing trip");
                return new MessageResponse<TripDto> { Success = false, Message = "An error occurred while completing the trip" };
            }
        }

        public async Task<MessageResponse<TripDto>> CancelTripAsync(CancelTripDto dto)
        {
            try
            {
                if (!_authUser.CompanyBranchId.HasValue) return new MessageResponse<TripDto> { Success = false, Message = "Invalid user context. Missing branch." };

                var now = DateTime.UtcNow;

                var trip = await _context.Trips
                    .FirstOrDefaultAsync(t => t.Id == dto.TripId &&
                                              t.CompanyBranchId == _authUser.CompanyBranchId &&
                                              t.IsActive);

                if (trip == null) return new MessageResponse<TripDto> { Success = false, Message = "Trip not found" };

                // Don't allow cancellation of completed trips
                if (trip.Status == TripStatus.Completed) return new MessageResponse<TripDto> { Success = false, Message = "Cannot cancel a completed trip" };

                // Cancel trip (keep assignment for audit — product decision; clear if desired)
                trip.Status = TripStatus.Cancelled;
                trip.CancellationReason = dto.CancellationReason;
                trip.CancellationDate = now;
                trip.ModifiedDate = now;
                trip.ModifiedBy = _authUser.UserId;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Trip cancelled", new { TripNumber = trip.TripNumber, UserId = _authUser.UserId });

                var result = await GetTripByIdAsync(trip.Id);
                return new MessageResponse<TripDto> { Success = true, Message = "Trip cancelled successfully", Result = result.Result };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling trip");
                return new MessageResponse<TripDto> { Success = false, Message = "An error occurred while cancelling the trip" };
            }
        }

        #endregion

        #region Approval Workflow

        public async Task<MessageResponse<TripDto>> ApproveTripAsync(ApproveTripDto dto)
        {
            try
            {
                if (!_authUser.CompanyBranchId.HasValue) return new MessageResponse<TripDto> { Success = false, Message = "Invalid user context. Missing branch." };

                var now = DateTime.UtcNow;

                var trip = await _context.Trips
                    .FirstOrDefaultAsync(t => t.Id == dto.TripId &&
                                              t.CompanyBranchId == _authUser.CompanyBranchId &&
                                              t.IsActive);

                if (trip == null) return new MessageResponse<TripDto> { Success = false, Message = "Trip not found" };

                if (!trip.RequiresApproval) return new MessageResponse<TripDto> { Success = false, Message = "This trip does not require approval" };

                if (trip.Status != TripStatus.PendingApproval) return new MessageResponse<TripDto> { Success = false, Message = $"Cannot approve trip with status '{trip.Status}'" };

                // Process approval inside transaction
                using var tx = await _context.Database.BeginTransactionAsync();
                try
                {
                    trip.IsApproved = dto.IsApproved;
                    trip.ApprovedBy = _authUser.UserId;
                    trip.ApprovedDate = now;
                    trip.ModifiedDate = now;
                    trip.ModifiedBy = _authUser.UserId;

                    if (dto.IsApproved)
                    {
                        trip.Status = trip.DriverId.HasValue ? TripStatus.Assigned : TripStatus.Approved;

                        if (!string.IsNullOrWhiteSpace(dto.Comments))
                        {
                            trip.Notes = string.IsNullOrWhiteSpace(trip.Notes) ? $"Approval Comments: {dto.Comments}" : $"{trip.Notes}\n\nApproval Comments: {dto.Comments}";
                        }
                    }
                    else
                    {
                        trip.Status = TripStatus.Rejected;
                        trip.RejectionReason = dto.Comments;
                    }

                    await _context.SaveChangesAsync();
                    await tx.CommitAsync();
                }
                catch
                {
                    await tx.RollbackAsync();
                    throw;
                }

                var action = dto.IsApproved ? "approved" : "rejected";
                _logger.LogInformation("Trip approval processed", new { TripNumber = trip.TripNumber, Action = action, UserId = _authUser.UserId });

                var result = await GetTripByIdAsync(trip.Id);
                return new MessageResponse<TripDto> { Success = true, Message = $"Trip {action} successfully", Result = result.Result };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving/rejecting trip");
                return new MessageResponse<TripDto> { Success = false, Message = "An error occurred while processing the approval" };
            }
        }

        public async Task<MessageResponse<PaginatedResult<TripListDto>>> GetPendingApprovalTripsAsync(int page, int pageSize)
        {
            try
            {
                var filter = new TripFilterDto { Status = TripStatus.PendingApproval, Page = page, PageSize = pageSize };
                return await GetTripsAsync(filter);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving pending approval trips");
                return new MessageResponse<PaginatedResult<TripListDto>> { Success = false, Message = "An error occurred while retrieving pending approval trips" };
            }
        }

        #endregion

        #region Trip Expenses

        public async Task<MessageResponse<TripExpenseDto>> AddTripExpenseAsync(CreateTripExpenseDto dto)
        {
            try
            {
                if (!_authUser.CompanyBranchId.HasValue) return new MessageResponse<TripExpenseDto> { Success = false, Message = "Invalid user context. Missing branch." };

                var trip = await _context.Trips
                    .FirstOrDefaultAsync(t => t.Id == dto.TripId &&
                                              t.CompanyBranchId == _authUser.CompanyBranchId &&
                                              t.IsActive);

                if (trip == null) return new MessageResponse<TripExpenseDto> { Success = false, Message = "Trip not found" };

                // Validate expense amount
                if (dto.Amount <= 0) return new MessageResponse<TripExpenseDto> { Success = false, Message = "Expense amount must be greater than zero" };

                // Default currency if not provided (business decision)
                //if (string.IsNullOrWhiteSpace(dto.Currency)) dto.Currency = "NGN";

                var now = DateTime.UtcNow;

                var expense = new TripExpense
                {
                    TripId = dto.TripId,
                    ExpenseType = dto.ExpenseType,
                    Description = dto.Description,
                    Amount = dto.Amount,
                    //Currency = dto.Currency,
                    ExpenseDate = dto.ExpenseDate,
                    ReceiptFileName = dto.ReceiptFileName,
                    IsVerified = false,
                    IsActive = true,
                    CreatedDate = now,
                    CreatedBy = _authUser.UserId
                };

                _context.TripExpenses.Add(expense);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Expense added to trip", new { TripNumber = trip.TripNumber, Amount = dto.Amount, UserId = _authUser.UserId });

                var expenseDto = new TripExpenseDto
                {
                    Id = expense.Id,
                    TripId = expense.TripId,
                    ExpenseType = expense.ExpenseType,
                    ExpenseTypeDisplay = expense.ExpenseType.ToString(),
                    Description = expense.Description,
                    Amount = expense.Amount,
                    Currency = expense.Currency,
                    ExpenseDate = expense.ExpenseDate,
                    ReceiptFileName = expense.ReceiptFileName,
                    ReceiptUrl = expense.ReceiptUrl,
                    IsVerified = expense.IsVerified,
                    CreatedDate = expense.CreatedDate
                };

                return new MessageResponse<TripExpenseDto> { Success = true, Message = "Expense added successfully", Result = expenseDto };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding trip expense");
                return new MessageResponse<TripExpenseDto> { Success = false, Message = "An error occurred while adding the expense" };
            }
        }

        public async Task<MessageResponse<List<TripExpenseDto>>> GetTripExpensesAsync(long tripId)
        {
            try
            {
                if (!_authUser.CompanyBranchId.HasValue) return new MessageResponse<List<TripExpenseDto>> { Success = false, Message = "Invalid user context. Missing branch." };

                var trip = await _context.Trips
                    .FirstOrDefaultAsync(t => t.Id == tripId &&
                                              t.CompanyBranchId == _authUser.CompanyBranchId &&
                                              t.IsActive);

                if (trip == null) return new MessageResponse<List<TripExpenseDto>> { Success = false, Message = "Trip not found" };

                var expenses = await _context.TripExpenses
                    .AsNoTracking()
                    .Where(e => e.TripId == tripId && e.IsActive)
                    .OrderByDescending(e => e.ExpenseDate)
                    .Select(e => new TripExpenseDto
                    {
                        Id = e.Id,
                        TripId = e.TripId,
                        ExpenseType = e.ExpenseType,
                        ExpenseTypeDisplay = e.ExpenseType.ToString(),
                        Description = e.Description,
                        Amount = e.Amount,
                        Currency = e.Currency,
                        ExpenseDate = e.ExpenseDate,
                        ReceiptFileName = e.ReceiptFileName,
                        ReceiptUrl = e.ReceiptUrl,
                        IsVerified = e.IsVerified,
                        VerifiedBy = e.VerifiedBy,
                        VerificationDate = e.VerificationDate,
                        CreatedDate = e.CreatedDate
                    })
                    .ToListAsync();

                return new MessageResponse<List<TripExpenseDto>> { Success = true, Result = expenses };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving trip expenses");
                return new MessageResponse<List<TripExpenseDto>> { Success = false, Message = "An error occurred while retrieving expenses" };
            }
        }

        public async Task<MessageResponse> DeleteTripExpenseAsync(long expenseId)
        {
            try
            {
                if (!_authUser.CompanyBranchId.HasValue) return new MessageResponse { Success = false, Message = "Invalid user context. Missing branch." };

                var expense = await _context.TripExpenses
                    .Include(e => e.Trip)
                    .FirstOrDefaultAsync(e => e.Id == expenseId &&
                                              e.Trip.CompanyBranchId == _authUser.CompanyBranchId &&
                                              e.IsActive);

                if (expense == null) return new MessageResponse { Success = false, Message = "Expense not found" };

                // Soft delete
                expense.IsActive = false;
                expense.ModifiedDate = DateTime.UtcNow;
                expense.ModifiedBy = _authUser.UserId;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Expense deleted from trip", new { ExpenseId = expenseId, TripId = expense.TripId, UserId = _authUser.UserId });

                return new MessageResponse { Success = true, Message = "Expense deleted successfully" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting trip expense");
                return new MessageResponse { Success = false, Message = "An error occurred while deleting the expense" };
            }
        }

        public async Task<MessageResponse<TripExpenseDto>> VerifyExpenseAsync(long expenseId)
        {
            try
            {
                if (!_authUser.CompanyBranchId.HasValue) return new MessageResponse<TripExpenseDto> { Success = false, Message = "Invalid user context. Missing branch." };

                var expense = await _context.TripExpenses
                    .Include(e => e.Trip)
                    .FirstOrDefaultAsync(e => e.Id == expenseId &&
                                              e.Trip.CompanyBranchId == _authUser.CompanyBranchId &&
                                              e.IsActive);

                if (expense == null) return new MessageResponse<TripExpenseDto> { Success = false, Message = "Expense not found" };

                if (expense.IsVerified) return new MessageResponse<TripExpenseDto> { Success = false, Message = "Expense is already verified" };

                expense.IsVerified = true;
                expense.VerifiedBy = _authUser.UserId;
                expense.VerificationDate = DateTime.UtcNow;
                expense.ModifiedDate = DateTime.UtcNow;
                expense.ModifiedBy = _authUser.UserId;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Expense verified", new { ExpenseId = expenseId, TripId = expense.TripId, UserId = _authUser.UserId });

                var expenseDto = new TripExpenseDto
                {
                    Id = expense.Id,
                    TripId = expense.TripId,
                    ExpenseType = expense.ExpenseType,
                    ExpenseTypeDisplay = expense.ExpenseType.ToString(),
                    Description = expense.Description,
                    Amount = expense.Amount,
                    Currency = expense.Currency,
                    ExpenseDate = expense.ExpenseDate,
                    ReceiptFileName = expense.ReceiptFileName,
                    ReceiptUrl = expense.ReceiptUrl,
                    IsVerified = expense.IsVerified,
                    VerifiedBy = expense.VerifiedBy,
                    VerificationDate = expense.VerificationDate,
                    CreatedDate = expense.CreatedDate
                };

                return new MessageResponse<TripExpenseDto> { Success = true, Message = "Expense verified successfully", Result = expenseDto };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying expense");
                return new MessageResponse<TripExpenseDto> { Success = false, Message = "An error occurred while verifying the expense" };
            }
        }

        #endregion

        #region Reports & Analytics

        public async Task<MessageResponse<TripStatistics>> GetTripStatisticsAsync(DateTime? startDate, DateTime? endDate)
        {
            try
            {
                if (!_authUser.CompanyBranchId.HasValue) return new MessageResponse<TripStatistics> { Success = false, Message = "Invalid user context. Missing branch." };

                var branchId = _authUser.CompanyBranchId.Value;
                var baseQuery = _context.Trips.AsNoTracking().Where(t => t.CompanyBranchId == branchId && t.IsActive);

                if (startDate.HasValue)
                {
                    baseQuery = baseQuery.Where(t => t.CreatedDate >= startDate.Value);
                }

                if (endDate.HasValue)
                {
                    baseQuery = baseQuery.Where(t => t.CreatedDate <= endDate.Value);
                }

                var total = await baseQuery.CountAsync();
                var scheduled = await baseQuery.CountAsync(t => t.Status == TripStatus.Scheduled || t.Status == TripStatus.Assigned);
                var active = await baseQuery.CountAsync(t => t.Status == TripStatus.InProgress);
                var completed = await baseQuery.CountAsync(t => t.Status == TripStatus.Completed);
                var cancelled = await baseQuery.CountAsync(t => t.Status == TripStatus.Cancelled);
                var pending = await baseQuery.CountAsync(t => t.Status == TripStatus.PendingApproval);

                var totalDistance = await baseQuery.Where(t => t.ActualDistance.HasValue).SumAsync(t => (double?)t.ActualDistance) ?? 0.0;
                var totalFuel = await baseQuery.Where(t => t.ActualFuelCost.HasValue).SumAsync(t => (decimal?)t.ActualFuelCost) ?? 0m;

                var now = DateTime.UtcNow;
                var weekStart = now.Date.AddDays(-(int)now.DayOfWeek);
                var monthStart = new DateTime(now.Year, now.Month, 1);

                var tripsThisWeek = await baseQuery.CountAsync(t => t.CreatedDate >= weekStart);
                var tripsThisMonth = await baseQuery.CountAsync(t => t.CreatedDate >= monthStart);

                double avgDistance = 0;
                decimal avgCost = 0m;

                var completedWithDistanceCount = await baseQuery.CountAsync(t => t.Status == TripStatus.Completed && t.ActualDistance.HasValue);
                if (completedWithDistanceCount > 0)
                {
                    avgDistance = (double)(await baseQuery.Where(t => t.Status == TripStatus.Completed && t.ActualDistance.HasValue).AverageAsync(t => (double?)t.ActualDistance) ?? 0);
                }

                var completedWithCostCount = await baseQuery.CountAsync(t => t.Status == TripStatus.Completed && t.ActualFuelCost.HasValue);
                if (completedWithCostCount > 0)
                {
                    avgCost = (decimal)(await baseQuery.Where(t => t.Status == TripStatus.Completed && t.ActualFuelCost.HasValue).AverageAsync(t => (decimal?)t.ActualFuelCost) ?? 0m);
                }

                var stats = new TripStatistics
                {
                    TotalTrips = total,
                    ScheduledTrips = scheduled,
                    ActiveTrips = active,
                    CompletedTrips = completed,
                    CancelledTrips = cancelled,
                    PendingApprovalTrips = pending,
                    TotalDistanceCovered = (decimal)totalDistance,
                    TotalFuelCost = totalFuel,
                    TripsThisWeek = tripsThisWeek,
                    TripsThisMonth = tripsThisMonth,
                    AverageTripDistance = (decimal)avgDistance,
                    AverageTripCost = avgCost
                };

                return new MessageResponse<TripStatistics> { Success = true, Result = stats };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving trip statistics");
                return new MessageResponse<TripStatistics> { Success = false, Message = "An error occurred while retrieving statistics" };
            }
        }

        public async Task<MessageResponse<TripDashboardViewModel>> GetDashboardDataAsync()
        {
            try
            {
                if (!_authUser.CompanyBranchId.HasValue) return new MessageResponse<TripDashboardViewModel> { Success = false, Message = "Invalid user context. Missing branch." };

                var now = DateTime.UtcNow;
                var statisticsResponse = await GetTripStatisticsAsync(null, null);

                if (!statisticsResponse.Success) return new MessageResponse<TripDashboardViewModel> { Success = false, Message = statisticsResponse.Message };

                var upcomingTrips = await _context.Trips
                    .AsNoTracking()
                    .Include(t => t.Vehicle)
                    .Include(t => t.Driver).ThenInclude(d => d.User)
                    .Where(t => t.CompanyBranchId == _authUser.CompanyBranchId && t.IsActive && (t.Status == TripStatus.Scheduled || t.Status == TripStatus.Assigned) && t.ScheduledStartDate >= now && t.ScheduledStartDate <= now.AddDays(7))
                    .OrderBy(t => t.ScheduledStartDate)
                    .Take(10)
                    .Select(t => new TripListDto
                    {
                        Id = t.Id,
                        TripNumber = t.TripNumber,
                        VehiclePlateNo = t.Vehicle != null ? t.Vehicle.PlateNo : null,
                        DriverName = t.Driver != null && t.Driver.User != null ? t.Driver.User.FirstName + " " + t.Driver.User.LastName : null,
                        Origin = t.Origin,
                        Destination = t.Destination,
                        ScheduledStartDate = t.ScheduledStartDate,
                        ScheduledEndDate = t.ScheduledEndDate,
                        Status = t.Status,
                        StatusDisplay = t.Status.ToString(),
                        Priority = t.Priority,
                        PriorityDisplay = t.Priority.ToString(),
                        CreatedDate = t.CreatedDate
                    })
                    .ToListAsync();

                var activeTrips = await _context.Trips
                    .AsNoTracking()
                    .Include(t => t.Vehicle)
                    .Include(t => t.Driver).ThenInclude(d => d.User)
                    .Where(t => t.CompanyBranchId == _authUser.CompanyBranchId && t.IsActive && t.Status == TripStatus.InProgress)
                    .OrderByDescending(t => t.ActualStartDate)
                    .Take(10)
                    .Select(t => new TripListDto
                    {
                        Id = t.Id,
                        TripNumber = t.TripNumber,
                        VehiclePlateNo = t.Vehicle != null ? t.Vehicle.PlateNo : null,
                        DriverName = t.Driver != null && t.Driver.User != null ? t.Driver.User.FirstName + " " + t.Driver.User.LastName : null,
                        Origin = t.Origin,
                        Destination = t.Destination,
                        ScheduledStartDate = t.ScheduledStartDate,
                        ScheduledEndDate = t.ScheduledEndDate,
                        Status = t.Status,
                        StatusDisplay = t.Status.ToString(),
                        Priority = t.Priority,
                        PriorityDisplay = t.Priority.ToString(),
                        CreatedDate = t.CreatedDate
                    })
                    .ToListAsync();

                var pendingApprovalTrips = await _context.Trips
                    .AsNoTracking()
                    .Include(t => t.Vehicle)
                    .Include(t => t.Driver).ThenInclude(d => d.User)
                    .Where(t => t.CompanyBranchId == _authUser.CompanyBranchId && t.IsActive && t.Status == TripStatus.PendingApproval)
                    .OrderByDescending(t => t.CreatedDate)
                    .Take(10)
                    .Select(t => new TripListDto
                    {
                        Id = t.Id,
                        TripNumber = t.TripNumber,
                        VehiclePlateNo = t.Vehicle != null ? t.Vehicle.PlateNo : null,
                        DriverName = t.Driver != null && t.Driver.User != null ? t.Driver.User.FirstName + " " + t.Driver.User.LastName : null,
                        Origin = t.Origin,
                        Destination = t.Destination,
                        ScheduledStartDate = t.ScheduledStartDate,
                        ScheduledEndDate = t.ScheduledEndDate,
                        Status = t.Status,
                        StatusDisplay = t.Status.ToString(),
                        Priority = t.Priority,
                        PriorityDisplay = t.Priority.ToString(),
                        CreatedDate = t.CreatedDate
                    })
                    .ToListAsync();

                var dashboard = new TripDashboardViewModel
                {
                    Statistics = statisticsResponse.Result,
                    UpcomingTrips = upcomingTrips,
                    ActiveTrips = activeTrips,
                    PendingApprovalTrips = pendingApprovalTrips
                };

                return new MessageResponse<TripDashboardViewModel> { Success = true, Result = dashboard };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving dashboard data");
                return new MessageResponse<TripDashboardViewModel> { Success = false, Message = "An error occurred while retrieving dashboard data" };
            }
        }

        public async Task<MessageResponse<List<TripListDto>>> GetDriverTripsAsync(long driverId, int page, int pageSize)
        {
            try
            {
                if (!_authUser.CompanyBranchId.HasValue) return new MessageResponse<List<TripListDto>> { Success = false, Message = "Invalid user context. Missing branch." };

                // Verify driver belongs to branch
                var driver = await _context.Drivers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(d => d.Id == driverId && d.CompanyBranchId == _authUser.CompanyBranchId && d.IsActive);

                if (driver == null) return new MessageResponse<List<TripListDto>> { Success = false, Message = "Driver not found" };

                var baseQuery = _context.Trips
                    .AsNoTracking()
                    .Include(t => t.Vehicle)
                    .Include(t => t.Driver).ThenInclude(d => d.User)
                    .Where(t => t.DriverId == driverId && t.IsActive);

                var trips = await baseQuery
                    .OrderByDescending(t => t.ScheduledStartDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(t => new TripListDto
                    {
                        Id = t.Id,
                        TripNumber = t.TripNumber,
                        VehiclePlateNo = t.Vehicle != null ? t.Vehicle.PlateNo : null,
                        DriverName = t.Driver != null && t.Driver.User != null ? t.Driver.User.FirstName + " " + t.Driver.User.LastName : null,
                        Origin = t.Origin,
                        Destination = t.Destination,
                        ScheduledStartDate = t.ScheduledStartDate,
                        ScheduledEndDate = t.ScheduledEndDate,
                        Status = t.Status,
                        StatusDisplay = t.Status.ToString(),
                        Priority = t.Priority,
                        PriorityDisplay = t.Priority.ToString(),
                        CreatedDate = t.CreatedDate
                    })
                    .ToListAsync();

                return new MessageResponse<List<TripListDto>> { Success = true, Result = trips };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving driver trips");
                return new MessageResponse<List<TripListDto>> { Success = false, Message = "An error occurred while retrieving driver trips" };
            }
        }

        public async Task<MessageResponse<List<TripListDto>>> GetVehicleTripsAsync(long vehicleId, int page, int pageSize)
        {
            try
            {
                if (!_authUser.CompanyBranchId.HasValue) return new MessageResponse<List<TripListDto>> { Success = false, Message = "Invalid user context. Missing branch." };

                // Verify vehicle belongs to branch
                var vehicle = await _context.Vehicles
                    .AsNoTracking()
                    .FirstOrDefaultAsync(v => v.Id == vehicleId && v.CompanyBranchId == _authUser.CompanyBranchId && v.IsActive);

                if (vehicle == null) return new MessageResponse<List<TripListDto>> { Success = false, Message = "Vehicle not found" };

                var baseQuery = _context.Trips
                    .AsNoTracking()
                    .Include(t => t.Vehicle)
                    .Include(t => t.Driver).ThenInclude(d => d.User)
                    .Where(t => t.VehicleId == vehicleId && t.IsActive);

                var trips = await baseQuery
                    .OrderByDescending(t => t.ScheduledStartDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(t => new TripListDto
                    {
                        Id = t.Id,
                        TripNumber = t.TripNumber,
                        VehiclePlateNo = t.Vehicle != null ? t.Vehicle.PlateNo : null,
                        DriverName = t.Driver != null && t.Driver.User != null ? t.Driver.User.FirstName + " " + t.Driver.User.LastName : null,
                        Origin = t.Origin,
                        Destination = t.Destination,
                        ScheduledStartDate = t.ScheduledStartDate,
                        ScheduledEndDate = t.ScheduledEndDate,
                        Status = t.Status,
                        StatusDisplay = t.Status.ToString(),
                        Priority = t.Priority,
                        PriorityDisplay = t.Priority.ToString(),
                        CreatedDate = t.CreatedDate
                    })
                    .ToListAsync();

                return new MessageResponse<List<TripListDto>> { Success = true, Result = trips };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving vehicle trips");
                return new MessageResponse<List<TripListDto>> { Success = false, Message = "An error occurred while retrieving vehicle trips" };
            }
        }

        #endregion

        #region Validation & Business Rules

        public async Task<MessageResponse<bool>> ValidateTripAvailabilityAsync(
            long vehicleId,
            long? driverId,
            DateTime startDate,
            DateTime endDate,
            long? excludeTripId = null)
        {
            try
            {
                if (!_authUser.CompanyBranchId.HasValue) return new MessageResponse<bool> { Success = false, Message = "Invalid user context. Missing branch.", Result = false };

                var branchId = _authUser.CompanyBranchId.Value;

                var activeStatuses = new[] { TripStatus.Scheduled, TripStatus.Assigned, TripStatus.Approved, TripStatus.InProgress };

                var vehicleConflictQuery = _context.Trips
                    .AsNoTracking()
                    .Where(t => t.VehicleId == vehicleId &&
                                t.CompanyBranchId == branchId &&
                                t.IsActive &&
                                activeStatuses.Contains(t.Status));

                if (excludeTripId.HasValue) vehicleConflictQuery = vehicleConflictQuery.Where(t => t.Id != excludeTripId.Value);

                var hasVehicleConflict = await vehicleConflictQuery.AnyAsync(t => t.ScheduledStartDate < endDate && t.ScheduledEndDate > startDate);

                if (hasVehicleConflict) return new MessageResponse<bool> { Success = false, Message = "Vehicle is already assigned to another trip during this period", Result = false };

                if (driverId.HasValue)
                {
                    var driverConflictQuery = _context.Trips
                        .AsNoTracking()
                        .Where(t => t.DriverId == driverId.Value &&
                                    t.CompanyBranchId == branchId &&
                                    t.IsActive &&
                                    activeStatuses.Contains(t.Status));

                    if (excludeTripId.HasValue) driverConflictQuery = driverConflictQuery.Where(t => t.Id != excludeTripId.Value);

                    var hasDriverConflict = await driverConflictQuery.AnyAsync(t => t.ScheduledStartDate < endDate && t.ScheduledEndDate > startDate);

                    if (hasDriverConflict) return new MessageResponse<bool> { Success = false, Message = "Driver is already assigned to another trip during this period", Result = false };
                }

                return new MessageResponse<bool> { Success = true, Message = "Vehicle and driver are available for the specified period", Result = true };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating trip availability");
                return new MessageResponse<bool> { Success = false, Message = "An error occurred while validating availability", Result = false };
            }
        }

        #endregion

        #region Helper Methods

        private async Task<string> GenerateTripNumberAsync()
        {
            var date = DateTime.UtcNow;
            var prefix = $"TRP{date:yyyyMMdd}";
            const int maxAttempts = 5;

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                var lastTrip = await _context.Trips
                    .AsNoTracking()
                    .Where(t => t.TripNumber.StartsWith(prefix))
                    .OrderByDescending(t => t.TripNumber)
                    .FirstOrDefaultAsync();

                int sequence = 1;
                if (lastTrip != null && lastTrip.TripNumber.Length > prefix.Length)
                {
                    var lastSeqStr = lastTrip.TripNumber.Substring(prefix.Length);
                    if (int.TryParse(lastSeqStr, out var lastNum)) sequence = lastNum + 1;
                }

                var candidate = $"{prefix}{sequence:D4}";

                var exists = await _context.Trips.AsNoTracking().AnyAsync(t => t.TripNumber == candidate);
                if (!exists) return candidate;

                await Task.Delay(50);
            }

            // As a final safety-net rely on DB unique index. If still failing, throw.
            throw new InvalidOperationException("Unable to generate unique trip number. Please try again.");
        }

        private TripDto MapTripToDto(Trip trip)
        {
            if (trip == null) return null;

            return new TripDto
            {
                Id = trip.Id,
                TripNumber = trip.TripNumber,
                CompanyBranchId = trip.CompanyBranchId,
                CompanyId = trip.CompanyId,

                VehicleId = trip.VehicleId,
                VehiclePlateNo = trip.Vehicle != null ? trip.Vehicle.PlateNo : null,
                VehicleMake = trip.Vehicle?.VehicleMake?.Name,
                VehicleModel = trip.Vehicle?.VehicleModel?.Name,

                DriverId = trip.DriverId,
                DriverName = trip.Driver != null && trip.Driver.User != null ? $"{trip.Driver.User.FirstName} {trip.Driver.User.LastName}" : null,
                DriverLicenseNumber = trip.Driver?.LicenseNumber,

                Origin = trip.Origin,
                Destination = trip.Destination,
                Purpose = trip.Purpose,
                Description = trip.Description,

                ScheduledStartDate = trip.ScheduledStartDate,
                ScheduledEndDate = trip.ScheduledEndDate,
                ActualStartDate = trip.ActualStartDate,
                ActualEndDate = trip.ActualEndDate,

                EstimatedDistance = trip.EstimatedDistance,
                ActualDistance = trip.ActualDistance,
                EstimatedFuelCost = trip.EstimatedFuelCost,
                ActualFuelCost = trip.ActualFuelCost,

                StartOdometer = trip.StartOdometer,
                EndOdometer = trip.EndOdometer,

                Status = trip.Status,
                StatusDisplay = trip.Status.ToString(),
                Priority = trip.Priority,
                PriorityDisplay = trip.Priority.ToString(),

                AssignedBy = trip.AssignedBy,
                AssignedDate = trip.AssignedDate,

                RequiresApproval = trip.RequiresApproval,
                IsApproved = trip.IsApproved,
                ApprovedBy = trip.ApprovedBy,
                ApprovedDate = trip.ApprovedDate,
                RejectionReason = trip.RejectionReason,

                Notes = trip.Notes,
                CancellationReason = trip.CancellationReason,
                CancellationDate = trip.CancellationDate,

                IsActive = trip.IsActive,
                CreatedDate = trip.CreatedDate,
                ModifiedDate = trip.ModifiedDate,
                CreatedBy = trip.CreatedBy,
                ModifiedBy = trip.ModifiedBy
            };
        }

        #endregion




    }










    //public class TripService : ITripService
    //{
    //    private readonly FleetManagerDbContext _context;
    //    private readonly IAuthUser _authUser;
    //    private readonly ILogger<TripService> _logger;

    //    public TripService( FleetManagerDbContext context, IAuthUser authUser,ILogger<TripService> logger)
    //    {
    //        _context = context;
    //        _authUser = authUser;
    //        _logger = logger;
    //    }

    //    #region CRUD Operations

    //    public async Task<MessageResponse<TripDto>> CreateTripAsync(CreateTripDto dto)
    //    {
    //        try
    //        {
    //            // Validate scheduled dates
    //            if (dto.ScheduledEndDate <= dto.ScheduledStartDate)
    //            {
    //                return new MessageResponse<TripDto>
    //                {
    //                    Success = false,
    //                    Message = "Scheduled end date must be after start date"
    //                };
    //            }

    //            // Validate vehicle exists and belongs to branch
    //            var vehicle = await _context.Vehicles
    //                .FirstOrDefaultAsync(v => v.Id == dto.VehicleId &&
    //                                        v.CompanyBranchId == _authUser.CompanyBranchId &&
    //                                        v.IsActive);

    //            if (vehicle == null)
    //            {
    //                return new MessageResponse<TripDto>
    //                {
    //                    Success = false,
    //                    Message = "Vehicle not found or not available in your branch"
    //                };
    //            }

    //            // Validate driver if provided
    //            if (dto.DriverId.HasValue)
    //            {
    //                var driver = await _context.Drivers
    //                    .FirstOrDefaultAsync(d => d.Id == dto.DriverId &&
    //                                            d.CompanyBranchId == _authUser.CompanyBranchId &&
    //                                            d.IsActive);

    //                if (driver == null)
    //                {
    //                    return new MessageResponse<TripDto>
    //                    {
    //                        Success = false,
    //                        Message = "Driver not found or not available in your branch"
    //                    };
    //                }

    //                // Check if driver license is valid
    //                if (driver.LicenseExpiryDate.HasValue && driver.LicenseExpiryDate.Value < DateTime.UtcNow)
    //                {
    //                    return new MessageResponse<TripDto>
    //                    {
    //                        Success = false,
    //                        Message = "Driver's license has expired"
    //                    };
    //                }
    //            }

    //            // Check availability
    //            var availabilityCheck = await ValidateTripAvailabilityAsync(
    //                dto.VehicleId,
    //                dto.DriverId,
    //                dto.ScheduledStartDate,
    //                dto.ScheduledEndDate);

    //            if (!availabilityCheck.Success)
    //            {
    //                return new MessageResponse<TripDto>
    //                {
    //                    Success = false,
    //                    Message = availabilityCheck.Message
    //                };
    //            }

    //            // Generate trip number
    //            var tripNumber = await GenerateTripNumberAsync();

    //            // Create trip entity
    //            var trip = new Trip
    //            {
    //                TripNumber = tripNumber,
    //                CompanyBranchId = _authUser.CompanyBranchId.Value,
    //                CompanyId = _authUser.CompanyId.Value,
    //                VehicleId = dto.VehicleId,
    //                DriverId = dto.DriverId,
    //                Origin = dto.Origin,
    //                Destination = dto.Destination,
    //                Purpose = dto.Purpose,
    //                Description = dto.Description,
    //                ScheduledStartDate = dto.ScheduledStartDate,
    //                ScheduledEndDate = dto.ScheduledEndDate,
    //                EstimatedDistance = dto.EstimatedDistance,
    //                EstimatedFuelCost = dto.EstimatedFuelCost,
    //                Priority = dto.Priority,
    //                RequiresApproval = dto.RequiresApproval,
    //                Status = dto.RequiresApproval ? TripStatus.PendingApproval : TripStatus.Scheduled,
    //                Notes = dto.Notes,
    //                IsActive = true,
    //                CreatedDate = DateTime.UtcNow,
    //                CreatedBy = _authUser.UserId
    //            };

    //            // If driver is assigned at creation
    //            if (dto.DriverId.HasValue)
    //            {
    //                trip.AssignedBy = _authUser.UserId;
    //                trip.AssignedDate = DateTime.UtcNow;
    //                trip.Status = dto.RequiresApproval ? TripStatus.PendingApproval : TripStatus.Assigned;
    //            }

    //            _context.Trips.Add(trip);
    //            await _context.SaveChangesAsync();

    //            _logger.LogInformation($"Trip {tripNumber} created by {_authUser.UserId}");

    //            var result = await GetTripByIdAsync(trip.Id);
    //            return new MessageResponse<TripDto>
    //            {
    //                Success = true,
    //                Message = "Trip created successfully",
    //                Result = result.Result
    //            };
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(ex, "Error creating trip");
    //            return new MessageResponse<TripDto>
    //            {
    //                Success = false,
    //                Message = "An error occurred while creating the trip"
    //            };
    //        }
    //    }

    //    public async Task<MessageResponse<TripDto>> UpdateTripAsync(UpdateTripDto dto)
    //    {
    //        try
    //        {
    //            var trip = await _context.Trips
    //                .FirstOrDefaultAsync(t => t.Id == dto.Id &&
    //                                        t.CompanyBranchId == _authUser.CompanyBranchId &&
    //                                        t.IsActive);

    //            if (trip == null)
    //            {
    //                return new MessageResponse<TripDto>
    //                {
    //                    Success = false,
    //                    Message = "Trip not found"
    //                };
    //            }

    //            // Don't allow updates to trips in certain statuses
    //            if (trip.Status == TripStatus.InProgress || trip.Status == TripStatus.Completed)
    //            {
    //                return new MessageResponse<TripDto>
    //                {
    //                    Success = false,
    //                    Message = $"Cannot update trip with status '{trip.Status}'"
    //                };
    //            }

    //            // Validate dates
    //            if (dto.ScheduledEndDate <= dto.ScheduledStartDate)
    //            {
    //                return new MessageResponse<TripDto>
    //                {
    //                    Success = false,
    //                    Message = "Scheduled end date must be after start date"
    //                };
    //            }

    //            // Validate vehicle
    //            var vehicle = await _context.Vehicles
    //                .FirstOrDefaultAsync(v => v.Id == dto.VehicleId &&
    //                                        v.CompanyBranchId == _authUser.CompanyBranchId &&
    //                                        v.IsActive);

    //            if (vehicle == null)
    //            {
    //                return new MessageResponse<TripDto>
    //                {
    //                    Success = false,
    //                    Message = "Vehicle not found"
    //                };
    //            }

    //            // Validate driver if provided
    //            if (dto.DriverId.HasValue)
    //            {
    //                var driver = await _context.Drivers
    //                    .FirstOrDefaultAsync(d => d.Id == dto.DriverId &&
    //                                            d.CompanyBranchId == _authUser.CompanyBranchId &&
    //                                            d.IsActive);

    //                if (driver == null)
    //                {
    //                    return new MessageResponse<TripDto>
    //                    {
    //                        Success = false,
    //                        Message = "Driver not found"
    //                    };
    //                }
    //            }

    //            // Check availability (exclude current trip)
    //            var availabilityCheck = await ValidateTripAvailabilityAsync(
    //                dto.VehicleId,
    //                dto.DriverId,
    //                dto.ScheduledStartDate,
    //                dto.ScheduledEndDate,
    //                dto.Id);

    //            if (!availabilityCheck.Success)
    //            {
    //                return new MessageResponse<TripDto>
    //                {
    //                    Success = false,
    //                    Message = availabilityCheck.Message
    //                };
    //            }

    //            // Update trip
    //            trip.VehicleId = dto.VehicleId;
    //            trip.DriverId = dto.DriverId;
    //            trip.Origin = dto.Origin;
    //            trip.Destination = dto.Destination;
    //            trip.Purpose = dto.Purpose;
    //            trip.Description = dto.Description;
    //            trip.ScheduledStartDate = dto.ScheduledStartDate;
    //            trip.ScheduledEndDate = dto.ScheduledEndDate;
    //            trip.EstimatedDistance = dto.EstimatedDistance;
    //            trip.EstimatedFuelCost = dto.EstimatedFuelCost;
    //            trip.Priority = dto.Priority;
    //            trip.RequiresApproval = dto.RequiresApproval;
    //            trip.Notes = dto.Notes;
    //            trip.ModifiedDate = DateTime.UtcNow;
    //            trip.ModifiedBy = _authUser.UserId;

    //            await _context.SaveChangesAsync();

    //            _logger.LogInformation($"Trip {trip.TripNumber} updated by {_authUser.UserId}");

    //            var result = await GetTripByIdAsync(trip.Id);
    //            return new MessageResponse<TripDto>
    //            {
    //                Success = true,
    //                Message = "Trip updated successfully",
    //                Result = result.Result
    //            };
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(ex, "Error updating trip");
    //            return new MessageResponse<TripDto>
    //            {
    //                Success = false,
    //                Message = "An error occurred while updating the trip"
    //            };
    //        }
    //    }

    //    public async Task<MessageResponse<TripDto>> GetTripByIdAsync(long id)
    //    {
    //        try
    //        {
    //            var trip = await _context.Trips
    //                .Include(t => t.Vehicle)
    //                    .ThenInclude(v => v.VehicleMake)
    //                .Include(t => t.Vehicle)
    //                    .ThenInclude(v => v.VehicleModel)
    //                .Include(t => t.Driver)
    //                    .ThenInclude(d => d.User)
    //                .FirstOrDefaultAsync(t => t.Id == id &&
    //                                        t.CompanyBranchId == _authUser.CompanyBranchId &&
    //                                        t.IsActive);

    //            if (trip == null)
    //            {
    //                return new MessageResponse<TripDto>
    //                {
    //                    Success = false,
    //                    Message = "Trip not found"
    //                };
    //            }

    //            var tripDto = MapTripToDto(trip);

    //            return new MessageResponse<TripDto>
    //            {
    //                Success = true,
    //                Result = tripDto
    //            };
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(ex, "Error retrieving trip");
    //            return new MessageResponse<TripDto>
    //            {
    //                Success = false,
    //                Message = "An error occurred while retrieving the trip"
    //            };
    //        }
    //    }

    //    public async Task<MessageResponse<PaginatedResult<TripListDto>>> GetTripsAsync(TripFilterDto filter)
    //    {
    //        try
    //        {
    //            var query = _context.Trips
    //                .Include(t => t.Vehicle)
    //                .Include(t => t.Driver)
    //                    .ThenInclude(d => d.User)
    //                .Where(t => t.CompanyBranchId == _authUser.CompanyBranchId && t.IsActive);

    //            // Apply filters
    //            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
    //            {
    //                var searchTerm = filter.SearchTerm.ToLower();
    //                query = query.Where(t =>
    //                    t.TripNumber.ToLower().Contains(searchTerm) ||
    //                    t.Origin.ToLower().Contains(searchTerm) ||
    //                    t.Destination.ToLower().Contains(searchTerm) ||
    //                t.Purpose.ToLower().Contains(searchTerm) ||
    //                t.Vehicle.PlateNo.ToLower().Contains(searchTerm));
    //            }

    //            if (filter.Status.HasValue)
    //            {
    //                query = query.Where(t => t.Status == filter.Status.Value);
    //            }

    //            if (filter.Priority.HasValue)
    //            {
    //                query = query.Where(t => t.Priority == filter.Priority.Value);
    //            }

    //            if (filter.DriverId.HasValue)
    //            {
    //                query = query.Where(t => t.DriverId == filter.DriverId.Value);
    //            }

    //            if (filter.VehicleId.HasValue)
    //            {
    //                query = query.Where(t => t.VehicleId == filter.VehicleId.Value);
    //            }

    //            if (filter.StartDate.HasValue)
    //            {
    //                query = query.Where(t => t.ScheduledStartDate >= filter.StartDate.Value);
    //            }

    //            if (filter.EndDate.HasValue)
    //            {
    //                query = query.Where(t => t.ScheduledEndDate <= filter.EndDate.Value);
    //            }

    //            // Order by scheduled start date descending
    //            query = query.OrderByDescending(t => t.ScheduledStartDate);

    //            // Get total count
    //            var totalCount = await query.CountAsync();

    //            // Apply pagination
    //            var trips = await query
    //                .Skip((filter.Page - 1) * filter.PageSize)
    //                .Take(filter.PageSize)
    //                .Select(t => new TripListDto
    //                {
    //                    Id = t.Id,
    //                    TripNumber = t.TripNumber,
    //                    VehiclePlateNo = t.Vehicle.PlateNo,
    //                    DriverName = t.Driver != null ? t.Driver.User.FirstName + " " + t.Driver.User.LastName : null,
    //                    Origin = t.Origin,
    //                    Destination = t.Destination,
    //                    ScheduledStartDate = t.ScheduledStartDate,
    //                    ScheduledEndDate = t.ScheduledEndDate,
    //                    Status = t.Status,
    //                    StatusDisplay = t.Status.ToString(),
    //                    Priority = t.Priority,
    //                    PriorityDisplay = t.Priority.ToString(),
    //                    CreatedDate = t.CreatedDate
    //                })
    //                .ToListAsync();

    //            var result = new PaginatedResult<TripListDto>
    //            {
    //                Items = trips,
    //                Page = filter.Page,
    //                PageSize = filter.PageSize,
    //                TotalCount = totalCount
    //            };

    //            return new MessageResponse<PaginatedResult<TripListDto>>
    //            {
    //                Success = true,
    //                Result = result
    //            };
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(ex, "Error retrieving trips");
    //            return new MessageResponse<PaginatedResult<TripListDto>>
    //            {
    //                Success = false,
    //                Message = "An error occurred while retrieving trips"
    //            };
    //        }
    //    }

    //    public async Task<MessageResponse> DeleteTripAsync(long id)
    //    {
    //        try
    //        {
    //            var trip = await _context.Trips
    //                .FirstOrDefaultAsync(t => t.Id == id &&
    //                                        t.CompanyBranchId == _authUser.CompanyBranchId &&
    //                                        t.IsActive);

    //            if (trip == null)
    //            {
    //                return new MessageResponse
    //                {
    //                    Success = false,
    //                    Message = "Trip not found"
    //                };
    //            }

    //            // Don't allow deletion of trips in certain statuses
    //            if (trip.Status == TripStatus.InProgress || trip.Status == TripStatus.Completed)
    //            {
    //                return new MessageResponse
    //                {
    //                    Success = false,
    //                    Message = $"Cannot delete trip with status '{trip.Status}'"
    //                };
    //            }

    //            // Soft delete
    //            trip.IsActive = false;
    //            trip.ModifiedDate = DateTime.UtcNow;
    //            trip.ModifiedBy = _authUser.UserId;

    //            await _context.SaveChangesAsync();

    //            _logger.LogInformation($"Trip {trip.TripNumber} deleted by {_authUser.UserId}");

    //            return new MessageResponse
    //            {
    //                Success = true,
    //                Message = "Trip deleted successfully"
    //            };
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(ex, "Error deleting trip");
    //            return new MessageResponse
    //            {
    //                Success = false,
    //                Message = "An error occurred while deleting the trip"
    //            };
    //        }
    //    }

    //    #endregion

    //    #region Trip Assignment & Management

    //    public async Task<MessageResponse<TripDto>> AssignTripToDriverAsync(AssignTripDto dto)
    //    {
    //        try
    //        {
    //            var trip = await _context.Trips
    //                .Include(t => t.Vehicle)
    //                .FirstOrDefaultAsync(t => t.Id == dto.TripId &&
    //                                        t.CompanyBranchId == _authUser.CompanyBranchId &&
    //                                        t.IsActive);

    //            if (trip == null)
    //            {
    //                return new MessageResponse<TripDto>
    //                {
    //                    Success = false,
    //                    Message = "Trip not found"
    //                };
    //            }

    //            // Check if trip can be assigned
    //            if (trip.Status != TripStatus.Scheduled && trip.Status != TripStatus.Approved)
    //            {
    //                return new MessageResponse<TripDto>
    //                {
    //                    Success = false,
    //                    Message = $"Cannot assign trip with status '{trip.Status}'"
    //                };
    //            }

    //            // Validate driver
    //            var driver = await _context.Drivers
    //                .Include(d => d.User)
    //                .FirstOrDefaultAsync(d => d.Id == dto.DriverId &&
    //                                        d.CompanyBranchId == _authUser.CompanyBranchId &&
    //                                        d.IsActive);

    //            if (driver == null)
    //            {
    //                return new MessageResponse<TripDto>
    //                {
    //                    Success = false,
    //                    Message = "Driver not found or not available in your branch"
    //                };
    //            }

    //            // Check driver status
    //            if (driver.EmploymentStatus == EmploymentStatus.Inactive)
    //            {
    //                return new MessageResponse<TripDto>
    //                {
    //                    Success = false,
    //                    Message = "Driver is not active"
    //                };
    //            }

    //            // Check if driver license is valid
    //            if (driver.LicenseExpiryDate.HasValue && driver.LicenseExpiryDate.Value < DateTime.UtcNow)
    //            {
    //                return new MessageResponse<TripDto>
    //                {
    //                    Success = false,
    //                    Message = "Driver's license has expired"
    //                };
    //            }

    //            // Check driver availability for the trip period
    //            var hasConflict = await _context.Trips
    //                .AnyAsync(t => t.DriverId == dto.DriverId &&
    //                             t.Id != dto.TripId &&
    //                             t.IsActive &&
    //                             (t.Status == TripStatus.Scheduled ||
    //                              t.Status == TripStatus.Assigned ||
    //                              t.Status == TripStatus.InProgress) &&
    //                             ((t.ScheduledStartDate <= trip.ScheduledEndDate &&
    //                               t.ScheduledEndDate >= trip.ScheduledStartDate)));

    //            if (hasConflict)
    //            {
    //                return new MessageResponse<TripDto>
    //                {
    //                    Success = false,
    //                    Message = "Driver is already assigned to another trip during this period"
    //                };
    //            }

    //            // Assign driver to trip
    //            trip.DriverId = dto.DriverId;
    //            trip.AssignedBy = _authUser.UserId;
    //            trip.AssignedDate = DateTime.UtcNow;
    //            trip.Status = TripStatus.Assigned;
    //            trip.ModifiedDate = DateTime.UtcNow;
    //            trip.ModifiedBy = _authUser.UserId;

    //            if (!string.IsNullOrWhiteSpace(dto.Notes))
    //            {
    //                trip.Notes = string.IsNullOrWhiteSpace(trip.Notes)
    //                    ? dto.Notes
    //                    : $"{trip.Notes}\n\nAssignment Notes: {dto.Notes}";
    //            }

    //            await _context.SaveChangesAsync();

    //            _logger.LogInformation($"Trip {trip.TripNumber} assigned to driver {driver.User.FirstName} {driver.User.LastName} by {_authUser.UserId}");

    //            var result = await GetTripByIdAsync(trip.Id);
    //            return new MessageResponse<TripDto>
    //            {
    //                Success = true,
    //                Message = $"Trip successfully assigned to {driver.User.FirstName} {driver.User.LastName}",
    //                Result = result.Result
    //            };
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(ex, "Error assigning trip to driver");
    //            return new MessageResponse<TripDto>
    //            {
    //                Success = false,
    //                Message = "An error occurred while assigning the trip"
    //            };
    //        }
    //    }

    //    public async Task<MessageResponse<TripDto>> UnassignTripAsync(long tripId)
    //    {
    //        try
    //        {
    //            var trip = await _context.Trips
    //                .FirstOrDefaultAsync(t => t.Id == tripId &&
    //                                        t.CompanyBranchId == _authUser.CompanyBranchId &&
    //                                        t.IsActive);

    //            if (trip == null)
    //            {
    //                return new MessageResponse<TripDto>
    //                {
    //                    Success = false,
    //                    Message = "Trip not found"
    //                };
    //            }

    //            // Check if trip can be unassigned
    //            if (trip.Status != TripStatus.Assigned && trip.Status != TripStatus.Scheduled)
    //            {
    //                return new MessageResponse<TripDto>
    //                {
    //                    Success = false,
    //                    Message = $"Cannot unassign trip with status '{trip.Status}'"
    //                };
    //            }

    //            if (!trip.DriverId.HasValue)
    //            {
    //                return new MessageResponse<TripDto>
    //                {
    //                    Success = false,
    //                    Message = "Trip is not assigned to any driver"
    //                };
    //            }

    //            // Unassign driver
    //            trip.DriverId = null;
    //            trip.AssignedBy = null;
    //            trip.AssignedDate = null;
    //            trip.Status = trip.RequiresApproval && trip.IsApproved == true
    //                ? TripStatus.Approved
    //                : TripStatus.Scheduled;
    //            trip.ModifiedDate = DateTime.UtcNow;
    //            trip.ModifiedBy = _authUser.UserId;

    //            await _context.SaveChangesAsync();

    //            _logger.LogInformation($"Trip {trip.TripNumber} unassigned by {_authUser.UserId}");

    //            var result = await GetTripByIdAsync(trip.Id);
    //            return new MessageResponse<TripDto>
    //            {
    //                Success = true,
    //                Message = "Driver unassigned from trip successfully",
    //                Result = result.Result
    //            };
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(ex, "Error unassigning trip");
    //            return new MessageResponse<TripDto>
    //            {
    //                Success = false,
    //                Message = "An error occurred while unassigning the trip"
    //            };
    //        }
    //    }

    //    public async Task<MessageResponse<TripDto>> StartTripAsync(StartTripDto dto)
    //    {
    //        try
    //        {
    //            var trip = await _context.Trips
    //                .Include(t => t.Vehicle)
    //                .Include(t => t.Driver)
    //                .FirstOrDefaultAsync(t => t.Id == dto.TripId &&
    //                                        t.CompanyBranchId == _authUser.CompanyBranchId &&
    //                                        t.IsActive);

    //            if (trip == null)
    //            {
    //                return new MessageResponse<TripDto>
    //                {
    //                    Success = false,
    //                    Message = "Trip not found"
    //                };
    //            }

    //            // Validate trip status
    //            if (trip.Status != TripStatus.Assigned)
    //            {
    //                return new MessageResponse<TripDto>
    //                {
    //                    Success = false,
    //                    Message = $"Cannot start trip with status '{trip.Status}'. Trip must be assigned first."
    //                };
    //            }

    //            if (!trip.DriverId.HasValue)
    //            {
    //                return new MessageResponse<TripDto>
    //                {
    //                    Success = false,
    //                    Message = "Trip must be assigned to a driver before starting"
    //                };
    //            }

    //            // Validate odometer reading
    //            if (trip.Vehicle.Mileage.HasValue && dto.StartOdometer < trip.Vehicle.Mileage.Value)
    //            {
    //                return new MessageResponse<TripDto>
    //                {
    //                    Success = false,
    //                    Message = $"Start odometer reading ({dto.StartOdometer} km) cannot be less than vehicle's current mileage ({trip.Vehicle.Mileage.Value} km)"
    //                };
    //            }

    //            // Start trip
    //            trip.ActualStartDate = DateTime.UtcNow;
    //            trip.StartOdometer = dto.StartOdometer;
    //            trip.Status = TripStatus.InProgress;
    //            trip.ModifiedDate = DateTime.UtcNow;
    //            trip.ModifiedBy = _authUser.UserId;

    //            // Update vehicle mileage
    //            trip.Vehicle.Mileage = dto.StartOdometer;

    //            // Update driver shift status
    //            if (trip.Driver != null)
    //            {
    //                trip.Driver.ShiftStatus = ShiftStatus.OnDuty;
    //                trip.Driver.LastSeen = DateTime.UtcNow;
    //            }

    //            // Create start checkpoint
    //            var checkpoint = new TripCheckpoint
    //            {
    //                TripId = trip.Id,
    //                Location = trip.Origin,
    //                Description = "Trip started",
    //                CheckpointTime = DateTime.UtcNow,
    //                CheckpointType = CheckpointType.Start,
    //                Latitude = dto.Latitude,
    //                Longitude = dto.Longitude,
    //                Notes = dto.Notes,
    //                IsActive = true,
    //                CreatedDate = DateTime.UtcNow,
    //                CreatedBy = _authUser.UserId
    //            };

    //            _context.TripCheckpoints.Add(checkpoint);
    //            await _context.SaveChangesAsync();

    //            _logger.LogInformation($"Trip {trip.TripNumber} started by driver {_authUser.UserId}");

    //            var result = await GetTripByIdAsync(trip.Id);
    //            return new MessageResponse<TripDto>
    //            {
    //                Success = true,
    //                Message = "Trip started successfully",
    //                Result = result.Result
    //            };
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(ex, "Error starting trip");
    //            return new MessageResponse<TripDto>
    //            {
    //                Success = false,
    //                Message = "An error occurred while starting the trip"
    //            };
    //        }
    //    }

    //    public async Task<MessageResponse<TripDto>> CompleteTripAsync(CompleteTripDto dto)
    //    {
    //        try
    //        {
    //            var trip = await _context.Trips
    //                .Include(t => t.Vehicle)
    //                .Include(t => t.Driver)
    //                .FirstOrDefaultAsync(t => t.Id == dto.TripId &&
    //                                        t.CompanyBranchId == _authUser.CompanyBranchId &&
    //                                        t.IsActive);

    //            if (trip == null)
    //            {
    //                return new MessageResponse<TripDto>
    //                {
    //                    Success = false,
    //                    Message = "Trip not found"
    //                };
    //            }

    //            // Validate trip status
    //            if (trip.Status != TripStatus.InProgress)
    //            {
    //                return new MessageResponse<TripDto>
    //                {
    //                    Success = false,
    //                    Message = $"Cannot complete trip with status '{trip.Status}'. Trip must be in progress."
    //                };
    //            }

    //            // Validate odometer reading
    //            if (!trip.StartOdometer.HasValue)
    //            {
    //                return new MessageResponse<TripDto>
    //                {
    //                    Success = false,
    //                    Message = "Trip does not have a start odometer reading"
    //                };
    //            }

    //            if (dto.EndOdometer <= trip.StartOdometer.Value)
    //            {
    //                return new MessageResponse<TripDto>
    //                {
    //                    Success = false,
    //                    Message = $"End odometer reading ({dto.EndOdometer} km) must be greater than start odometer ({trip.StartOdometer.Value} km)"
    //                };
    //            }

    //            // Complete trip
    //            trip.ActualEndDate = DateTime.UtcNow;
    //            trip.EndOdometer = dto.EndOdometer;
    //            trip.ActualDistance = dto.EndOdometer - trip.StartOdometer.Value;
    //            trip.ActualFuelCost = dto.ActualFuelCost;
    //            trip.Status = TripStatus.Completed;
    //            trip.ModifiedDate = DateTime.UtcNow;
    //            trip.ModifiedBy = _authUser.UserId;

    //            // Update vehicle mileage
    //            trip.Vehicle.Mileage = dto.EndOdometer;

    //            // Update driver shift status
    //            if (trip.Driver != null)
    //            {
    //                trip.Driver.ShiftStatus = ShiftStatus.Available;
    //                trip.Driver.LastSeen = DateTime.UtcNow;
    //            }

    //            // Create end checkpoint
    //            var checkpoint = new TripCheckpoint
    //            {
    //                TripId = trip.Id,
    //                Location = trip.Destination,
    //                Description = "Trip completed",
    //                CheckpointTime = DateTime.UtcNow,
    //                CheckpointType = CheckpointType.End,
    //                Latitude = dto.Latitude,
    //                Longitude = dto.Longitude,
    //                Notes = dto.Notes,
    //                IsActive = true,
    //                CreatedDate = DateTime.UtcNow,
    //                CreatedBy = _authUser.UserId
    //            };

    //            _context.TripCheckpoints.Add(checkpoint);
    //            await _context.SaveChangesAsync();

    //            _logger.LogInformation($"Trip {trip.TripNumber} completed. Distance: {trip.ActualDistance} km");

    //            var result = await GetTripByIdAsync(trip.Id);
    //            return new MessageResponse<TripDto>
    //            {
    //                Success = true,
    //                Message = $"Trip completed successfully. Distance covered: {trip.ActualDistance} km",
    //                Result = result.Result
    //            };
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(ex, "Error completing trip");
    //            return new MessageResponse<TripDto>
    //            {
    //                Success = false,
    //                Message = "An error occurred while completing the trip"
    //            };
    //        }
    //    }

    //    public async Task<MessageResponse<TripDto>> CancelTripAsync(CancelTripDto dto)
    //    {
    //        try
    //        {
    //            var trip = await _context.Trips
    //                .FirstOrDefaultAsync(t => t.Id == dto.TripId &&
    //                                        t.CompanyBranchId == _authUser.CompanyBranchId &&
    //                                        t.IsActive);

    //            if (trip == null)
    //            {
    //                return new MessageResponse<TripDto>
    //                {
    //                    Success = false,
    //                    Message = "Trip not found"
    //                };
    //            }

    //            // Don't allow cancellation of completed trips
    //            if (trip.Status == TripStatus.Completed)
    //            {
    //                return new MessageResponse<TripDto>
    //                {
    //                    Success = false,
    //                    Message = "Cannot cancel a completed trip"
    //                };
    //            }

    //            // Cancel trip
    //            trip.Status = TripStatus.Cancelled;
    //            trip.CancellationReason = dto.CancellationReason;
    //            trip.CancellationDate = DateTime.UtcNow;
    //            trip.ModifiedDate = DateTime.UtcNow;
    //            trip.ModifiedBy = _authUser.UserId;

    //            await _context.SaveChangesAsync();

    //            _logger.LogInformation($"Trip {trip.TripNumber} cancelled by {_authUser.UserId}. Reason: {dto.CancellationReason}");

    //            var result = await GetTripByIdAsync(trip.Id);
    //            return new MessageResponse<TripDto>
    //            {
    //                Success = true,
    //                Message = "Trip cancelled successfully",
    //                Result = result.Result
    //            };
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(ex, "Error cancelling trip");
    //            return new MessageResponse<TripDto>
    //            {
    //                Success = false,
    //                Message = "An error occurred while cancelling the trip"
    //            };
    //        }
    //    }

    //    #endregion

    //    #region Approval Workflow

    //    public async Task<MessageResponse<TripDto>> ApproveTripAsync(ApproveTripDto dto)
    //    {
    //        try
    //        {
    //            var trip = await _context.Trips
    //                .FirstOrDefaultAsync(t => t.Id == dto.TripId &&
    //                                        t.CompanyBranchId == _authUser.CompanyBranchId &&
    //                                        t.IsActive);

    //            if (trip == null)
    //            {
    //                return new MessageResponse<TripDto>
    //                {
    //                    Success = false,
    //                    Message = "Trip not found"
    //                };
    //            }

    //            if (!trip.RequiresApproval)
    //            {
    //                return new MessageResponse<TripDto>
    //                {
    //                    Success = false,
    //                    Message = "This trip does not require approval"
    //                };
    //            }

    //            if (trip.Status != TripStatus.PendingApproval)
    //            {
    //                return new MessageResponse<TripDto>
    //                {
    //                    Success = false,
    //                    Message = $"Cannot approve trip with status '{trip.Status}'"
    //                };
    //            }

    //            // Process approval
    //            trip.IsApproved = dto.IsApproved;
    //            trip.ApprovedBy = _authUser.UserId;
    //            trip.ApprovedDate = DateTime.UtcNow;
    //            trip.ModifiedDate = DateTime.UtcNow;
    //            trip.ModifiedBy = _authUser.UserId;

    //            if (dto.IsApproved)
    //            {
    //                trip.Status = trip.DriverId.HasValue ? TripStatus.Assigned : TripStatus.Approved;

    //                if (!string.IsNullOrWhiteSpace(dto.Comments))
    //                {
    //                    trip.Notes = string.IsNullOrWhiteSpace(trip.Notes)
    //                        ? $"Approval Comments: {dto.Comments}"
    //                        : $"{trip.Notes}\n\nApproval Comments: {dto.Comments}";
    //                }
    //            }
    //            else
    //            {
    //                trip.Status = TripStatus.Rejected;
    //                trip.RejectionReason = dto.Comments;
    //            }

    //            await _context.SaveChangesAsync();

    //            var action = dto.IsApproved ? "approved" : "rejected";
    //            _logger.LogInformation($"Trip {trip.TripNumber} {action} by {_authUser.UserId}");

    //            var result = await GetTripByIdAsync(trip.Id);
    //            return new MessageResponse<TripDto>
    //            {
    //                Success = true,
    //                Message = $"Trip {action} successfully",
    //                Result = result.Result
    //            };
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(ex, "Error approving/rejecting trip");
    //            return new MessageResponse<TripDto>
    //            {
    //                Success = false,
    //                Message = "An error occurred while processing the approval"
    //            };
    //        }
    //    }

    //    public async Task<MessageResponse<PaginatedResult<TripListDto>>> GetPendingApprovalTripsAsync(int page, int pageSize)
    //    {
    //        try
    //        {
    //            var filter = new TripFilterDto
    //            {
    //                Status = TripStatus.PendingApproval,
    //                Page = page,
    //                PageSize = pageSize
    //            };

    //            return await GetTripsAsync(filter);
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(ex, "Error retrieving pending approval trips");
    //            return new MessageResponse<PaginatedResult<TripListDto>>
    //            {
    //                Success = false,
    //                Message = "An error occurred while retrieving pending approval trips"
    //            };
    //        }
    //    }

    //    #endregion

    //    #region Trip Expenses

    //    public async Task<MessageResponse<TripExpenseDto>> AddTripExpenseAsync(CreateTripExpenseDto dto)
    //    {
    //        try
    //        {
    //            var trip = await _context.Trips
    //                .FirstOrDefaultAsync(t => t.Id == dto.TripId &&
    //                                        t.CompanyBranchId == _authUser.CompanyBranchId &&
    //                                        t.IsActive);

    //            if (trip == null)
    //            {
    //                return new MessageResponse<TripExpenseDto>
    //                {
    //                    Success = false,
    //                    Message = "Trip not found"
    //                };
    //            }

    //            // Create expense
    //            var expense = new TripExpense
    //            {
    //                TripId = dto.TripId,
    //                ExpenseType = dto.ExpenseType,
    //                Description = dto.Description,
    //                Amount = dto.Amount,
    //                ExpenseDate = dto.ExpenseDate,
    //                ReceiptFileName = dto.ReceiptFileName,
    //                IsVerified = false,
    //                IsActive = true,
    //                CreatedDate = DateTime.UtcNow,
    //                CreatedBy = _authUser.UserId
    //            };

    //            _context.TripExpenses.Add(expense);
    //            await _context.SaveChangesAsync();

    //            _logger.LogInformation($"Expense added to trip {trip.TripNumber}: {dto.Description} - {dto.Amount}");

    //            var expenseDto = new TripExpenseDto
    //            {
    //                Id = expense.Id,
    //                TripId = expense.TripId,
    //                ExpenseType = expense.ExpenseType,
    //                ExpenseTypeDisplay = expense.ExpenseType.ToString(),
    //                Description = expense.Description,
    //                Amount = expense.Amount,
    //                Currency = expense.Currency,
    //                ExpenseDate = expense.ExpenseDate,
    //                ReceiptFileName = expense.ReceiptFileName,
    //                ReceiptUrl = expense.ReceiptUrl,
    //                IsVerified = expense.IsVerified,
    //                CreatedDate = expense.CreatedDate
    //            };

    //            return new MessageResponse<TripExpenseDto>
    //            {
    //                Success = true,
    //                Message = "Expense added successfully",
    //                Result = expenseDto
    //            };
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(ex, "Error adding trip expense");
    //            return new MessageResponse<TripExpenseDto>
    //            {
    //                Success = false,
    //                Message = "An error occurred while adding the expense"
    //            };
    //        }
    //    }

    //    public async Task<MessageResponse<List<TripExpenseDto>>> GetTripExpensesAsync(long tripId)
    //    {
    //        try
    //        {
    //            var trip = await _context.Trips
    //                .FirstOrDefaultAsync(t => t.Id == tripId &&
    //                                        t.CompanyBranchId == _authUser.CompanyBranchId &&
    //                                        t.IsActive);

    //            if (trip == null)
    //            {
    //                return new MessageResponse<List<TripExpenseDto>>
    //                {
    //                    Success = false,
    //                    Message = "Trip not found"
    //                };
    //            }

    //            var expenses = await _context.TripExpenses
    //                .Where(e => e.TripId == tripId && e.IsActive)
    //                .OrderByDescending(e => e.ExpenseDate)
    //                .Select(e => new TripExpenseDto
    //                {
    //                    Id = e.Id,
    //                    TripId = e.TripId,
    //                    ExpenseType = e.ExpenseType,
    //                    ExpenseTypeDisplay = e.ExpenseType.ToString(),
    //                    Description = e.Description,
    //                    Amount = e.Amount,
    //                    Currency = e.Currency,
    //                    ExpenseDate = e.ExpenseDate,
    //                    ReceiptFileName = e.ReceiptFileName,
    //                    ReceiptUrl = e.ReceiptUrl,
    //                    IsVerified = e.IsVerified,
    //                    VerifiedBy = e.VerifiedBy,
    //                    VerificationDate = e.VerificationDate,
    //                    CreatedDate = e.CreatedDate
    //                })
    //                .ToListAsync();

    //            return new MessageResponse<List<TripExpenseDto>>
    //            {
    //                Success = true,
    //                Result = expenses
    //            };
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(ex, "Error retrieving trip expenses");
    //            return new MessageResponse<List<TripExpenseDto>>
    //            {
    //                Success = false,
    //                Message = "An error occurred while retrieving expenses"
    //            };
    //        }
    //    }

    //    public async Task<MessageResponse> DeleteTripExpenseAsync(long expenseId)
    //    {
    //        try
    //        {
    //            var expense = await _context.TripExpenses
    //                .Include(e => e.Trip)
    //                .FirstOrDefaultAsync(e => e.Id == expenseId &&
    //                                         e.Trip.CompanyBranchId == _authUser.CompanyBranchId &&
    //                                         e.IsActive);

    //            if (expense == null)
    //            {
    //                return new MessageResponse
    //                {
    //                    Success = false,
    //                    Message = "Expense not found"
    //                };
    //            }

    //            // Soft delete
    //            expense.IsActive = false;
    //            expense.ModifiedDate = DateTime.UtcNow;
    //            expense.ModifiedBy = _authUser.UserId;

    //            await _context.SaveChangesAsync();

    //            _logger.LogInformation($"Expense {expenseId} deleted from trip {expense.TripId}");

    //            return new MessageResponse
    //            {
    //                Success = true,
    //                Message = "Expense deleted successfully"
    //            };
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(ex, "Error deleting trip expense");
    //            return new MessageResponse
    //            {
    //                Success = false,
    //                Message = "An error occurred while deleting the expense"
    //            };
    //        }
    //    }

    //    public async Task<MessageResponse<TripExpenseDto>> VerifyExpenseAsync(long expenseId)
    //    {
    //        try
    //        {
    //            var expense = await _context.TripExpenses
    //                .Include(e => e.Trip)
    //                .FirstOrDefaultAsync(e => e.Id == expenseId &&
    //                                         e.Trip.CompanyBranchId == _authUser.CompanyBranchId &&
    //                                         e.IsActive);

    //            if (expense == null)
    //            {
    //                return new MessageResponse<TripExpenseDto>
    //                {
    //                    Success = false,
    //                    Message = "Expense not found"
    //                };
    //            }

    //            if (expense.IsVerified)
    //            {
    //                return new MessageResponse<TripExpenseDto>
    //                {
    //                    Success = false,
    //                    Message = "Expense is already verified"
    //                };
    //            }

    //            expense.IsVerified = true;
    //            expense.VerifiedBy = _authUser.UserId;
    //            expense.VerificationDate = DateTime.UtcNow;
    //            expense.ModifiedDate = DateTime.UtcNow;
    //            expense.ModifiedBy = _authUser.UserId;

    //            await _context.SaveChangesAsync();

    //            _logger.LogInformation($"Expense {expenseId} verified by {_authUser.UserId}");

    //            var expenseDto = new TripExpenseDto
    //            {
    //                Id = expense.Id,
    //                TripId = expense.TripId,
    //                ExpenseType = expense.ExpenseType,
    //                ExpenseTypeDisplay = expense.ExpenseType.ToString(),
    //                Description = expense.Description,
    //                Amount = expense.Amount,
    //                Currency = expense.Currency,
    //                ExpenseDate = expense.ExpenseDate,
    //                ReceiptFileName = expense.ReceiptFileName,
    //                ReceiptUrl = expense.ReceiptUrl,
    //                IsVerified = expense.IsVerified,
    //                VerifiedBy = expense.VerifiedBy,
    //                VerificationDate = expense.VerificationDate,
    //                CreatedDate = expense.CreatedDate
    //            };

    //            return new MessageResponse<TripExpenseDto>
    //            {
    //                Success = true,
    //                Message = "Expense verified successfully",
    //                Result = expenseDto
    //            };
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(ex, "Error verifying expense");
    //        }
    //        return new MessageResponse<TripExpenseDto>
    //        {
    //            Success = false,
    //            Message = "An error occurred while verifying the expense"
    //        };
    //    }


    //#endregion

    //#region Reports & Analytics

    //public async Task<MessageResponse<TripStatistics>> GetTripStatisticsAsync(DateTime? startDate, DateTime? endDate)
    //    {
    //        try
    //        {
    //            var query = _context.Trips
    //                .Where(t => t.CompanyBranchId == _authUser.CompanyBranchId && t.IsActive);

    //            // Apply date filters if provided
    //            if (startDate.HasValue)
    //            {
    //                query = query.Where(t => t.CreatedDate >= startDate.Value);
    //            }

    //            if (endDate.HasValue)
    //            {
    //                query = query.Where(t => t.CreatedDate <= endDate.Value);
    //            }

    //            var trips = await query.ToListAsync();

    //            var now = DateTime.UtcNow;
    //            var weekStart = now.AddDays(-(int)now.DayOfWeek);
    //            var monthStart = new DateTime(now.Year, now.Month, 1);

    //            var statistics = new TripStatistics
    //            {
    //                TotalTrips = trips.Count,
    //                ScheduledTrips = trips.Count(t => t.Status == TripStatus.Scheduled || t.Status == TripStatus.Assigned),
    //                ActiveTrips = trips.Count(t => t.Status == TripStatus.InProgress),
    //                CompletedTrips = trips.Count(t => t.Status == TripStatus.Completed),
    //                CancelledTrips = trips.Count(t => t.Status == TripStatus.Cancelled),
    //                PendingApprovalTrips = trips.Count(t => t.Status == TripStatus.PendingApproval),

    //                TotalDistanceCovered = trips.Where(t => t.ActualDistance.HasValue).Sum(t => t.ActualDistance.Value),
    //                TotalFuelCost = trips.Where(t => t.ActualFuelCost.HasValue).Sum(t => t.ActualFuelCost.Value),

    //                TripsThisWeek = trips.Count(t => t.CreatedDate >= weekStart),
    //                TripsThisMonth = trips.Count(t => t.CreatedDate >= monthStart)
    //            };

    //            var completedTrips = trips.Where(t => t.Status == TripStatus.Completed && t.ActualDistance.HasValue).ToList();
    //            if (completedTrips.Any())
    //            {
    //                statistics.AverageTripDistance = completedTrips.Average(t => t.ActualDistance.Value);

    //                var tripsWithCost = completedTrips.Where(t => t.ActualFuelCost.HasValue).ToList();
    //                if (tripsWithCost.Any())
    //                {
    //                    statistics.AverageTripCost = tripsWithCost.Average(t => t.ActualFuelCost.Value);
    //                }
    //            }

    //            return new MessageResponse<TripStatistics>
    //            {
    //                Success = true,
    //                Result = statistics
    //            };
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(ex, "Error retrieving trip statistics");
    //            return new MessageResponse<TripStatistics>
    //            {
    //                Success = false,
    //                Message = "An error occurred while retrieving statistics"
    //            };
    //        }
    //    }

    //    public async Task<MessageResponse<TripDashboardViewModel>> GetDashboardDataAsync()
    //    {
    //        try
    //        {
    //            var now = DateTime.UtcNow;
    //            var statisticsResponse = await GetTripStatisticsAsync(null, null);

    //            if (!statisticsResponse.Success)
    //            {
    //                return new MessageResponse<TripDashboardViewModel>
    //                {
    //                    Success = false,
    //                    Message = statisticsResponse.Message
    //                };
    //            }

    //            // Get upcoming trips (next 7 days)
    //            var upcomingTrips = await _context.Trips
    //                .Include(t => t.Vehicle)
    //                .Include(t => t.Driver)
    //                    .ThenInclude(d => d.User)
    //                .Where(t => t.CompanyBranchId == _authUser.CompanyBranchId &&
    //                           t.IsActive &&
    //                           (t.Status == TripStatus.Scheduled || t.Status == TripStatus.Assigned) &&
    //                           t.ScheduledStartDate >= now &&
    //                           t.ScheduledStartDate <= now.AddDays(7))
    //                .OrderBy(t => t.ScheduledStartDate)
    //                .Take(10)
    //                .Select(t => new TripListDto
    //                {
    //                    Id = t.Id,
    //                    TripNumber = t.TripNumber,
    //                    VehiclePlateNo = t.Vehicle.PlateNo,
    //                    DriverName = t.Driver != null ? t.Driver.User.FirstName + " " + t.Driver.User.LastName : null,
    //                    Origin = t.Origin,
    //                    Destination = t.Destination,
    //                    ScheduledStartDate = t.ScheduledStartDate,
    //                    ScheduledEndDate = t.ScheduledEndDate,
    //                    Status = t.Status,
    //                    StatusDisplay = t.Status.ToString(),
    //                    Priority = t.Priority,
    //                    PriorityDisplay = t.Priority.ToString(),
    //                    CreatedDate = t.CreatedDate
    //                })
    //                .ToListAsync();

    //            // Get active trips
    //            var activeTrips = await _context.Trips
    //                .Include(t => t.Vehicle)
    //                .Include(t => t.Driver)
    //                    .ThenInclude(d => d.User)
    //                .Where(t => t.CompanyBranchId == _authUser.CompanyBranchId &&
    //                           t.IsActive &&
    //                           t.Status == TripStatus.InProgress)
    //                .OrderByDescending(t => t.ActualStartDate)
    //                .Take(10)
    //                .Select(t => new TripListDto
    //                {
    //                    Id = t.Id,
    //                    TripNumber = t.TripNumber,
    //                    VehiclePlateNo = t.Vehicle.PlateNo,
    //                    DriverName = t.Driver != null ? t.Driver.User.FirstName + " " + t.Driver.User.LastName : null,
    //                    Origin = t.Origin,
    //                    Destination = t.Destination,
    //                    ScheduledStartDate = t.ScheduledStartDate,
    //                    ScheduledEndDate = t.ScheduledEndDate,
    //                    Status = t.Status,
    //                    StatusDisplay = t.Status.ToString(),
    //                    Priority = t.Priority,
    //                    PriorityDisplay = t.Priority.ToString(),
    //                    CreatedDate = t.CreatedDate
    //                })
    //                .ToListAsync();

    //            // Get pending approval trips
    //            var pendingApprovalTrips = await _context.Trips
    //                .Include(t => t.Vehicle)
    //                .Include(t => t.Driver)
    //                    .ThenInclude(d => d.User)
    //                .Where(t => t.CompanyBranchId == _authUser.CompanyBranchId &&
    //                           t.IsActive &&
    //                           t.Status == TripStatus.PendingApproval)
    //                .OrderByDescending(t => t.CreatedDate)
    //                .Take(10)
    //                .Select(t => new TripListDto
    //                {
    //                    Id = t.Id,
    //                    TripNumber = t.TripNumber,
    //                    VehiclePlateNo = t.Vehicle.PlateNo,
    //                    DriverName = t.Driver != null ? t.Driver.User.FirstName + " " + t.Driver.User.LastName : null,
    //                    Origin = t.Origin,
    //                    Destination = t.Destination,
    //                    ScheduledStartDate = t.ScheduledStartDate,
    //                    ScheduledEndDate = t.ScheduledEndDate,
    //                    Status = t.Status,
    //                    StatusDisplay = t.Status.ToString(),
    //                    Priority = t.Priority,
    //                    PriorityDisplay = t.Priority.ToString(),
    //                    CreatedDate = t.CreatedDate
    //                })
    //                .ToListAsync();

    //            var dashboard = new TripDashboardViewModel
    //            {
    //                Statistics = statisticsResponse.Result,
    //                UpcomingTrips = upcomingTrips,
    //                ActiveTrips = activeTrips,
    //                PendingApprovalTrips = pendingApprovalTrips
    //            };

    //            return new MessageResponse<TripDashboardViewModel>
    //            {
    //                Success = true,
    //                Result = dashboard
    //            };
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(ex, "Error retrieving dashboard data");
    //            return new MessageResponse<TripDashboardViewModel>
    //            {
    //                Success = false,
    //                Message = "An error occurred while retrieving dashboard data"
    //            };
    //        }
    //    }

    //    public async Task<MessageResponse<List<TripListDto>>> GetDriverTripsAsync(long driverId, int page, int pageSize)
    //    {
    //        try
    //        {
    //            // Verify driver belongs to branch
    //            var driver = await _context.Drivers
    //                .FirstOrDefaultAsync(d => d.Id == driverId &&
    //                                        d.CompanyBranchId == _authUser.CompanyBranchId &&
    //                                        d.IsActive);

    //            if (driver == null)
    //            {
    //                return new MessageResponse<List<TripListDto>>
    //                {
    //                    Success = false,
    //                    Message = "Driver not found"
    //                };
    //            }

    //            var trips = await _context.Trips
    //                .Include(t => t.Vehicle)
    //                .Include(t => t.Driver)
    //                    .ThenInclude(d => d.User)
    //                .Where(t => t.DriverId == driverId && t.IsActive)
    //                .OrderByDescending(t => t.ScheduledStartDate)
    //                .Skip((page - 1) * pageSize)
    //                .Take(pageSize)
    //                .Select(t => new TripListDto
    //                {
    //                    Id = t.Id,
    //                    TripNumber = t.TripNumber,
    //                    VehiclePlateNo = t.Vehicle.PlateNo,
    //                    DriverName = t.Driver.User.FirstName + " " + t.Driver.User.LastName,
    //                    Origin = t.Origin,
    //                    Destination = t.Destination,
    //                    ScheduledStartDate = t.ScheduledStartDate,
    //                    ScheduledEndDate = t.ScheduledEndDate,
    //                    Status = t.Status,
    //                    StatusDisplay = t.Status.ToString(),
    //                    Priority = t.Priority,
    //                    PriorityDisplay = t.Priority.ToString(),
    //                    CreatedDate = t.CreatedDate
    //                })
    //                .ToListAsync();

    //            return new MessageResponse<List<TripListDto>>
    //            {
    //                Success = true,
    //                Result = trips
    //            };
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(ex, "Error retrieving driver trips");
    //            return new MessageResponse<List<TripListDto>>
    //            {
    //                Success = false,
    //                Message = "An error occurred while retrieving driver trips"
    //            };
    //        }
    //    }

    //    public async Task<MessageResponse<List<TripListDto>>> GetVehicleTripsAsync(long vehicleId, int page, int pageSize)
    //    {
    //        try
    //        {
    //            // Verify vehicle belongs to branch
    //            var vehicle = await _context.Vehicles
    //                .FirstOrDefaultAsync(v => v.Id == vehicleId &&
    //                                        v.CompanyBranchId == _authUser.CompanyBranchId &&
    //                                        v.IsActive);

    //            if (vehicle == null)
    //            {
    //                return new MessageResponse<List<TripListDto>>
    //                {
    //                    Success = false,
    //                    Message = "Vehicle not found"
    //                };
    //            }

    //            var trips = await _context.Trips
    //                .Include(t => t.Vehicle)
    //                .Include(t => t.Driver)
    //                    .ThenInclude(d => d.User)
    //                .Where(t => t.VehicleId == vehicleId && t.IsActive)
    //                .OrderByDescending(t => t.ScheduledStartDate)
    //                .Skip((page - 1) * pageSize)
    //                .Take(pageSize)
    //                .Select(t => new TripListDto
    //                {
    //                    Id = t.Id,
    //                    TripNumber = t.TripNumber,
    //                    VehiclePlateNo = t.Vehicle.PlateNo,
    //                    DriverName = t.Driver != null ? t.Driver.User.FirstName + " " + t.Driver.User.LastName : null,
    //                    Origin = t.Origin,
    //                    Destination = t.Destination,
    //                    ScheduledStartDate = t.ScheduledStartDate,
    //                    ScheduledEndDate = t.ScheduledEndDate,
    //                    Status = t.Status,
    //                    StatusDisplay = t.Status.ToString(),
    //                    Priority = t.Priority,
    //                    PriorityDisplay = t.Priority.ToString(),
    //                    CreatedDate = t.CreatedDate
    //                })
    //                .ToListAsync();

    //            return new MessageResponse<List<TripListDto>>
    //            {
    //                Success = true,
    //                Result = trips
    //            };
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(ex, "Error retrieving vehicle trips");
    //            return new MessageResponse<List<TripListDto>>
    //            {
    //                Success = false,
    //                Message = "An error occurred while retrieving vehicle trips"
    //            };
    //        }
    //    }

    //    #endregion

    //    #region Validation & Business Rules

    //    public async Task<MessageResponse<bool>> ValidateTripAvailabilityAsync(
    //        long vehicleId,
    //        long? driverId,
    //        DateTime startDate,
    //        DateTime endDate,
    //        long? excludeTripId = null)
    //    {
    //        try
    //        {
    //            // Check vehicle availability
    //            var vehicleConflictQuery = _context.Trips
    //                .Where(t => t.VehicleId == vehicleId &&
    //                           t.CompanyBranchId == _authUser.CompanyBranchId &&
    //                           t.IsActive &&
    //                           (t.Status == TripStatus.Scheduled ||
    //                            t.Status == TripStatus.Assigned ||
    //                            t.Status == TripStatus.Approved ||
    //                            t.Status == TripStatus.InProgress) &&
    //                           ((t.ScheduledStartDate <= endDate && t.ScheduledEndDate >= startDate)));

    //            if (excludeTripId.HasValue)
    //            {
    //                vehicleConflictQuery = vehicleConflictQuery.Where(t => t.Id != excludeTripId.Value);
    //            }

    //            var hasVehicleConflict = await vehicleConflictQuery.AnyAsync();

    //            if (hasVehicleConflict)
    //            {
    //                return new MessageResponse<bool>
    //                {
    //                    Success = false,
    //                    Message = "Vehicle is already assigned to another trip during this period",
    //                    Result = false
    //                };
    //            }

    //            // Check driver availability if driver is specified
    //            if (driverId.HasValue)
    //            {
    //                var driverConflictQuery = _context.Trips
    //                    .Where(t => t.DriverId == driverId.Value &&
    //                               t.CompanyBranchId == _authUser.CompanyBranchId &&
    //                               t.IsActive &&
    //                               (t.Status == TripStatus.Scheduled ||
    //                                t.Status == TripStatus.Assigned ||
    //                                t.Status == TripStatus.Approved ||
    //                                t.Status == TripStatus.InProgress) &&
    //                               ((t.ScheduledStartDate <= endDate && t.ScheduledEndDate >= startDate)));

    //                if (excludeTripId.HasValue)
    //                {
    //                    driverConflictQuery = driverConflictQuery.Where(t => t.Id != excludeTripId.Value);
    //                }

    //                var hasDriverConflict = await driverConflictQuery.AnyAsync();

    //                if (hasDriverConflict)
    //                {
    //                    return new MessageResponse<bool>
    //                    {
    //                        Success = false,
    //                        Message = "Driver is already assigned to another trip during this period",
    //                        Result = false
    //                    };
    //                }
    //            }

    //            return new MessageResponse<bool>
    //            {
    //                Success = true,
    //                Message = "Vehicle and driver are available for the specified period",
    //                Result = true
    //            };
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(ex, "Error validating trip availability");
    //            return new MessageResponse<bool>
    //            {
    //                Success = false,
    //                Message = "An error occurred while validating availability",
    //                Result = false
    //            };
    //        }
    //    }

    //    #endregion

    //    #region Helper Methods

    //    private async Task<string> GenerateTripNumberAsync()
    //    {
    //        var date = DateTime.UtcNow;
    //        var prefix = $"TRP{date:yyyyMMdd}";

    //        var lastTrip = await _context.Trips
    //            .Where(t => t.TripNumber.StartsWith(prefix))
    //            .OrderByDescending(t => t.TripNumber)
    //            .FirstOrDefaultAsync();

    //        int sequence = 1;
    //        if (lastTrip != null && lastTrip.TripNumber.Length > prefix.Length)
    //        {
    //            var lastSequence = lastTrip.TripNumber.Substring(prefix.Length);
    //            if (int.TryParse(lastSequence, out int lastNumber))
    //            {
    //                sequence = lastNumber + 1;
    //            }
    //        }

    //        return $"{prefix}{sequence:D4}";
    //    }

    //    private TripDto MapTripToDto(Trip trip)
    //    {
    //        return new TripDto
    //        {
    //            Id = trip.Id,
    //            TripNumber = trip.TripNumber,
    //            CompanyBranchId = trip.CompanyBranchId,
    //            CompanyId = trip.CompanyId,

    //            VehicleId = trip.VehicleId,
    //            VehiclePlateNo = trip.Vehicle?.PlateNo,
    //            VehicleMake = trip.Vehicle?.VehicleMake?.Name,
    //            VehicleModel = trip.Vehicle?.VehicleModel?.Name,

    //            DriverId = trip.DriverId,
    //            DriverName = trip.Driver != null ? $"{trip.Driver.User.FirstName} {trip.Driver.User.LastName}" : null,
    //            DriverLicenseNumber = trip.Driver?.LicenseNumber,

    //            Origin = trip.Origin,
    //            Destination = trip.Destination,
    //            Purpose = trip.Purpose,
    //            Description = trip.Description,

    //            ScheduledStartDate = trip.ScheduledStartDate,
    //            ScheduledEndDate = trip.ScheduledEndDate,
    //            ActualStartDate = trip.ActualStartDate,
    //            ActualEndDate = trip.ActualEndDate,

    //            EstimatedDistance = trip.EstimatedDistance,
    //            ActualDistance = trip.ActualDistance,
    //            EstimatedFuelCost = trip.EstimatedFuelCost,
    //            ActualFuelCost = trip.ActualFuelCost,

    //            StartOdometer = trip.StartOdometer,
    //            EndOdometer = trip.EndOdometer,

    //            Status = trip.Status,
    //            StatusDisplay = trip.Status.ToString(),
    //            Priority = trip.Priority,
    //            PriorityDisplay = trip.Priority.ToString(),

    //            AssignedBy = trip.AssignedBy,
    //            AssignedDate = trip.AssignedDate,

    //            RequiresApproval = trip.RequiresApproval,
    //            IsApproved = trip.IsApproved,
    //            ApprovedBy = trip.ApprovedBy,
    //            ApprovedDate = trip.ApprovedDate,
    //            RejectionReason = trip.RejectionReason,

    //            Notes = trip.Notes,
    //            CancellationReason = trip.CancellationReason,
    //            CancellationDate = trip.CancellationDate,

    //            IsActive = trip.IsActive,
    //            CreatedDate = trip.CreatedDate,
    //            ModifiedDate = trip.ModifiedDate,
    //            CreatedBy = trip.CreatedBy,
    //            ModifiedBy = trip.ModifiedBy
    //        };
    //    }

    //    #endregion
    //} 
}