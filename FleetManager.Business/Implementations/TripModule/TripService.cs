using FleetManager.Business.Database.Entities;
using FleetManager.Business.Database.Entities.MaintenanceTicket;
using FleetManager.Business.DataObjects.TripsDto;
using FleetManager.Business.Enums;
using FleetManager.Business.Interfaces.NotificationModule;
using FleetManager.Business.Interfaces.TripModule;
using FleetManager.Business.Interfaces.UserModule;
using FleetManager.Business.UtilityModels;
using FleetManager.Business.ViewModels.TripsViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Hangfire;
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
        private readonly ILogger<TripService> _logger;
        private readonly INotificationService _notification;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly IAuthUser _authUser;

        public TripService(
            FleetManagerDbContext context,
            ILogger<TripService> logger,
            INotificationService notification,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            IBackgroundJobClient backgroundJobClient,
            IAuthUser authUser)
        {
            _context = context;
            _logger = logger;
            _notification = notification;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _backgroundJobClient = backgroundJobClient;
            _authUser = authUser;
        }

        private void EnsureDriverOnly()
        {
            var roles = (_authUser.Roles ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries).Select(r => r.Trim());
            if (roles.Contains("Company Admin") && roles.Contains("Company Owner") && roles.Contains("Super Admin"))
            {
                throw new UnauthorizedAccessException("Only the assigned driver may start/complete this trip");

            }
        }

        #region CRUD Operations

        public async Task<MessageResponse<TripDto>> CreateTripAsync(CreateTripDto dto)
        {
            try
            {
                if (_authUser?.CompanyBranchId == null || _authUser?.CompanyId == null)
                    return new MessageResponse<TripDto> { Success = false, Message = "Invalid user context. Missing branch or company." };

                var now = DateTime.UtcNow;

                // Normalize incoming client-local datetimes to UTC
                dto.ScheduledStartDate = DateTimeUtils.ToUtcFromLocal(dto.ScheduledStartDate);
                dto.ScheduledEndDate = DateTimeUtils.ToUtcFromLocal(dto.ScheduledEndDate);

                // Validate scheduled dates (use UTC assumption)
                if (dto.ScheduledEndDate <= dto.ScheduledStartDate)
                {
                    return new MessageResponse<TripDto> { Success = false, Message = "Scheduled end date must be after start date" };
                }

                // Validate driver if provided (driver must exist in branch)
                if (dto.DriverId.HasValue)
                {
                    var driver = await _context.Drivers
                        .AsNoTracking()
                        .FirstOrDefaultAsync(d => d.Id == dto.DriverId.Value &&
                                                  d.CompanyBranchId == _authUser.CompanyBranchId &&
                                                  d.IsActive);

                    if (driver == null)
                    {
                        return new MessageResponse<TripDto> { Success = false, Message = "Driver not found or not available in your branch" };
                    }

                    // Check license validity
                    if (driver.LicenseExpiryDate.HasValue && driver.LicenseExpiryDate.Value.Date < now.Date)
                    {
                        return new MessageResponse<TripDto> { Success = false, Message = "Driver's license has expired" };
                    }
                }

                // Validate vehicle exists and belongs to branch (we still check vehicleId even if we will also verify assignment)
                var vehicle = await _context.Vehicles
                    .AsNoTracking()
                    .FirstOrDefaultAsync(v => v.Id == dto.VehicleId &&
                                              v.CompanyBranchId == _authUser.CompanyBranchId &&
                                              v.IsActive);

                if (vehicle == null)
                {
                    return new MessageResponse<TripDto> { Success = false, Message = "Vehicle not found or not available in your branch" };
                }

                // If driver provided, ensure vehicle is assigned to the driver for the trip period (DriverVehicle)
                if (dto.DriverId.HasValue)
                {
                    var windowStart = dto.ScheduledStartDate;
                    var windowEnd = dto.ScheduledEndDate;

                    var assignment = await _context.DriverVehicles
                        .AsNoTracking()
                        .Where(dv => dv.DriverId == dto.DriverId.Value && dv.VehicleId == dto.VehicleId)
                        .Where(dv =>
                            // StartDate is null => assigned from -inf; else must start <= windowEnd
                            (dv.StartDate == null || dv.StartDate <= windowEnd) &&
                            // EndDate is null => assigned to +inf; else must end >= windowStart
                            (dv.EndDate == null || dv.EndDate >= windowStart))
                        .FirstOrDefaultAsync();

                    if (assignment == null)
                    {
                        return new MessageResponse<TripDto>
                        {
                            Success = false,
                            Message = "Selected vehicle is not assigned to the chosen driver for the trip period"
                        };
                    }
                }

                // Check availability (vehicle and driver) for the requested window
                var availabilityCheck = await ValidateTripAvailabilityAsync(dto.VehicleId, dto.DriverId, dto.ScheduledStartDate, dto.ScheduledEndDate);

                if (!availabilityCheck.Success)
                {
                    return new MessageResponse<TripDto> { Success = false, Message = availabilityCheck.Message };
                }

                // Generate unique trip number with retry to avoid concurrency collisions
                string tripNumber = null;
                const int maxAttempts = 3;
                for (var attempt = 1; attempt <= maxAttempts; attempt++)
                {
                    try
                    {
                        tripNumber = await GenerateTripNumberAsync();
                        break;
                    }
                    catch (DbUpdateException dex)
                    {
                        _logger.LogWarning(dex, "TripNumber generation collision on attempt {Attempt}", attempt);
                        if (attempt == maxAttempts) throw;
                        // small backoff
                        await Task.Delay(50 * attempt);
                    }
                }

                // Create Trip entity (CreatedBy stored as string to match your existing schema)
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

                if (dto.DriverId.HasValue)
                {
                    trip.AssignedBy = _authUser.UserId;
                    trip.AssignedDate = now;
                    trip.Status = dto.RequiresApproval ? TripStatus.PendingApproval : TripStatus.Assigned;
                }

                // Use a transaction when adding trip (good practice in case you later add creation of related entities)
                using (var tx = await _context.Database.BeginTransactionAsync())
                {
                    _context.Trips.Add(trip);
                    await _context.SaveChangesAsync();

                    await tx.CommitAsync();
                }

                _logger.LogInformation("Trip {TripNumber} created by {UserId}", trip.TripNumber, _authUser.UserId);

                // Enqueue assignment/created notification if driver assigned
                if (dto.DriverId.HasValue)
                {
                    try
                    {
                        var correlationId = Guid.NewGuid().ToString();
                        _backgroundJobClient.Enqueue<NotificationWorker>(w => w.ProcessEvent("TripAssigned", trip.Id, correlationId));
                    }
                    catch (Exception enqEx)
                    {
                        _logger.LogWarning(enqEx, "Failed to enqueue TripAssigned job for trip {TripId}", trip.Id);
                    }
                }

                var result = await GetTripByIdAsync(trip.Id);
                return new MessageResponse<TripDto> { Success = true, Message = "Trip created successfully", Result = result.Result };
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
                if (_authUser?.CompanyBranchId == null)
                    return new MessageResponse<TripDto> { Success = false, Message = "Invalid user context. Missing branch." };

                var now = DateTime.UtcNow;

                // Normalize incoming client-local datetimes to UTC
                dto.ScheduledStartDate = DateTimeUtils.ToUtcFromLocal(dto.ScheduledStartDate);
                dto.ScheduledEndDate = DateTimeUtils.ToUtcFromLocal(dto.ScheduledEndDate);

                var trip = await _context.Trips
                    .FirstOrDefaultAsync(t => t.Id == dto.Id &&
                                              t.CompanyBranchId == _authUser.CompanyBranchId &&
                                              t.IsActive);

                if (trip == null)
                {
                    return new MessageResponse<TripDto> { Success = false, Message = "Trip not found" };
                }

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

                if (vehicle == null)
                {
                    return new MessageResponse<TripDto> { Success = false, Message = "Vehicle not found" };
                }

                // Validate driver if provided
                if (dto.DriverId.HasValue)
                {
                    var driver = await _context.Drivers
                        .AsNoTracking()
                        .FirstOrDefaultAsync(d => d.Id == dto.DriverId.Value &&
                                                  d.CompanyBranchId == _authUser.CompanyBranchId &&
                                                  d.IsActive);

                    if (driver == null)
                    {
                        return new MessageResponse<TripDto> { Success = false, Message = "Driver not found" };
                    }
                }

                // Check availability (exclude current trip)
                var availabilityCheck = await ValidateTripAvailabilityAsync(dto.VehicleId, dto.DriverId, dto.ScheduledStartDate, dto.ScheduledEndDate, dto.Id);

                if (!availabilityCheck.Success)
                {
                    return new MessageResponse<TripDto> { Success = false, Message = availabilityCheck.Message };
                }

                // Update trip fields
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

                _logger.LogInformation("Trip {TripNumber} updated by {UserId}", trip.TripNumber, _authUser.UserId);

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
                if (_authUser?.CompanyBranchId == null) return new MessageResponse<TripDto> { Success = false, Message = "Invalid user context. Missing branch." };

                var trip = await _context.Trips
                    .AsNoTracking()
                    .Include(t => t.Vehicle).ThenInclude(v => v.VehicleMake)
                    .Include(t => t.Vehicle).ThenInclude(v => v.VehicleModel)
                    .Include(t => t.Driver).ThenInclude(d => d.User)
                    .Include(t => t.TripCheckpoints)
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
                if (_authUser?.CompanyBranchId == null) return new MessageResponse<PaginatedResult<TripListDto>> { Success = false, Message = "Invalid user context. Missing branch." };

                var query = _context.Trips
                    .AsNoTracking()
                    .Include(t => t.Vehicle).ThenInclude(v => v.VehicleMake)
                    .Include(t => t.Vehicle).ThenInclude(v => v.VehicleModel)
                    .Include(t => t.Driver).ThenInclude(d => d.User)
                    .Where(t => t.CompanyBranchId == _authUser.CompanyBranchId && t.IsActive);

                // Apply filters
                if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
                {
                    var searchTerm = filter.SearchTerm.ToLower();
                    query = query.Where(t =>
                        t.TripNumber.ToLower().Contains(searchTerm) ||
                        t.Origin.ToLower().Contains(searchTerm) ||
                        t.Destination.ToLower().Contains(searchTerm) ||
                        t.Purpose.ToLower().Contains(searchTerm) ||
                        t.Vehicle.PlateNo.ToLower().Contains(searchTerm));
                }

                if (filter.Status.HasValue) query = query.Where(t => t.Status == filter.Status.Value);
                if (filter.Priority.HasValue) query = query.Where(t => t.Priority == filter.Priority.Value);
                if (filter.DriverId.HasValue) query = query.Where(t => t.DriverId == filter.DriverId.Value);
                if (filter.VehicleId.HasValue) query = query.Where(t => t.VehicleId == filter.VehicleId.Value);
                if (filter.StartDate.HasValue) query = query.Where(t => t.ScheduledStartDate >= filter.StartDate.Value);
                if (filter.EndDate.HasValue) query = query.Where(t => t.ScheduledEndDate <= filter.EndDate.Value);

                // Order and pagination: improve over large offset by limiting page sizes
                query = query.OrderByDescending(t => t.ScheduledStartDate);

                var totalCount = await query.CountAsync();

                var trips = await query
                    .Skip((filter.Page - 1) * filter.PageSize)
                    .Take(filter.PageSize)
                    .Select(t => new TripListDto
                    {
                        Id = t.Id,
                        TripNumber = t.TripNumber,
                        VehiclePlateNo = t.Vehicle.VehicleMake.Name + " " + t.Vehicle.VehicleModel.Name + " " + t.Vehicle.PlateNo,
                        DriverName = t.Driver != null ? t.Driver.User.FirstName + " " + t.Driver.User.LastName : null,
                        Origin = t.Origin,
                        Destination = t.Destination,
                        ScheduledStartDate = t.ScheduledStartDate,
                        ScheduledEndDate = t.ScheduledEndDate,
                        Status = t.Status,
                        StatusDisplay = t.Status.ToString(),
                        Priority = t.Priority,
                        PriorityDisplay = t.Priority.ToString(),
                        CreatedDate = t.CreatedDate,
                        RequiresApproval = t.RequiresApproval

                    })
                    .ToListAsync();

                var result = new PaginatedResult<TripListDto>
                {
                    Items = trips,
                    Page = filter.Page,
                    PageSize = filter.PageSize,
                    TotalCount = totalCount
                };

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
                if (_authUser?.CompanyBranchId == null) return new MessageResponse { Success = false, Message = "Invalid user context. Missing branch." };

                var trip = await _context.Trips
                    .FirstOrDefaultAsync(t => t.Id == id &&
                                              t.CompanyBranchId == _authUser.CompanyBranchId &&
                                              t.IsActive);

                if (trip == null) return new MessageResponse { Success = false, Message = "Trip not found" };

                if (trip.Status == TripStatus.InProgress || trip.Status == TripStatus.Completed)
                {
                    return new MessageResponse { Success = false, Message = $"Cannot delete trip with status '{trip.Status}'" };
                }

                trip.IsActive = false;
                trip.ModifiedDate = DateTime.UtcNow;
                trip.ModifiedBy = _authUser.UserId;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Trip {TripNumber} deleted by {UserId}", trip.TripNumber, _authUser.UserId);
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
                if (_authUser?.CompanyBranchId == null) return new MessageResponse<TripDto> { Success = false, Message = "Invalid user context. Missing branch." };
                var now = DateTime.UtcNow;

                var trip = await _context.Trips
                    .Include(t => t.Vehicle)
                    .Include(t => t.Driver).ThenInclude(d => d.User)
                    .FirstOrDefaultAsync(t => t.Id == dto.TripId &&
                                              t.CompanyBranchId == _authUser.CompanyBranchId &&
                                              t.IsActive);

                if (trip == null) return new MessageResponse<TripDto> { Success = false, Message = "Trip not found" };

                if (trip.Status != TripStatus.Scheduled && trip.Status != TripStatus.Approved)
                {
                    return new MessageResponse<TripDto> { Success = false, Message = $"Cannot assign trip with status '{trip.Status}'" };
                }

                var driver = await _context.Drivers
                    .Include(d => d.User)
                    .FirstOrDefaultAsync(d => d.Id == dto.DriverId &&
                                              d.CompanyBranchId == _authUser.CompanyBranchId &&
                                              d.IsActive);

                if (driver == null) return new MessageResponse<TripDto> { Success = false, Message = "Driver not found or not available in your branch" };

                if (driver.EmploymentStatus == EmploymentStatus.Inactive)
                {
                    return new MessageResponse<TripDto> { Success = false, Message = "Driver is not active" };
                }

                if (driver.LicenseExpiryDate.HasValue && driver.LicenseExpiryDate.Value.Date < now.Date)
                {
                    return new MessageResponse<TripDto> { Success = false, Message = "Driver's license has expired" };
                }

                var hasConflict = await _context.Trips
                    .AsNoTracking()
                    .AnyAsync(t => t.DriverId == dto.DriverId &&
                                   t.Id != dto.TripId &&
                                   t.IsActive &&
                                   (t.Status == TripStatus.Scheduled || t.Status == TripStatus.Assigned || t.Status == TripStatus.InProgress) &&
                                   (t.ScheduledStartDate < trip.ScheduledEndDate && t.ScheduledEndDate > trip.ScheduledStartDate));

                if (hasConflict) return new MessageResponse<TripDto> { Success = false, Message = "Driver is already assigned to another trip during this period" };

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
                        trip.Notes = string.IsNullOrWhiteSpace(trip.Notes) ? dto.Notes : $"{trip.Notes}\n\nAssignment Notes: {dto.Notes}";

                    await _context.SaveChangesAsync();
                    await tx.CommitAsync();
                }
                catch
                {
                    await tx.RollbackAsync();
                    throw;
                }

                // Enqueue background job for notifications / webhook processing
                try
                {
                    var correlationId = Guid.NewGuid().ToString();
                    _backgroundJobClient.Enqueue<NotificationWorker>(w => w.ProcessEvent("TripAssigned", trip.Id, correlationId));
                }
                catch (Exception bgEx)
                {
                    _logger.LogWarning(bgEx, "Failed to enqueue TripAssigned job for trip {TripId}", trip.Id);
                }

                _logger.LogInformation("Trip {TripNumber} assigned to driver {DriverId} by {UserId}", trip.TripNumber, dto.DriverId, _authUser.UserId);

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
                if (_authUser?.CompanyBranchId == null) return new MessageResponse<TripDto> { Success = false, Message = "Invalid user context. Missing branch." };
                var now = DateTime.UtcNow;

                var trip = await _context.Trips
                    .Include(t => t.Driver).ThenInclude(d => d.User)
                    .FirstOrDefaultAsync(t => t.Id == tripId &&
                                              t.CompanyBranchId == _authUser.CompanyBranchId &&
                                              t.IsActive);

                if (trip == null) return new MessageResponse<TripDto> { Success = false, Message = "Trip not found" };

                if (trip.Status != TripStatus.Assigned && trip.Status != TripStatus.Scheduled)
                    return new MessageResponse<TripDto> { Success = false, Message = $"Cannot unassign trip with status '{trip.Status}'" };

                if (!trip.DriverId.HasValue) return new MessageResponse<TripDto> { Success = false, Message = "Trip is not assigned to any driver" };

                string? oldDriverUserId = trip.Driver?.UserId;
                var oldDriverName = trip.Driver != null ? $"{trip.Driver.User?.FirstName} {trip.Driver.User?.LastName}" : "Driver";

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

                // Enqueue background job for unassignment notifications
                try
                {
                    var correlationId = Guid.NewGuid().ToString();
                    _backgroundJobClient.Enqueue<NotificationWorker>(w => w.ProcessEvent("TripUnassigned", trip.Id, correlationId));
                }
                catch (Exception bgEx)
                {
                    _logger.LogWarning(bgEx, "Failed to enqueue TripUnassigned job for trip {TripId}", trip.Id);
                }

                _logger.LogInformation("Trip {TripNumber} unassigned by {UserId}", trip.TripNumber, _authUser.UserId);
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
            EnsureDriverOnly();
            try
            {
                if (_authUser?.CompanyBranchId == null)
                    return new MessageResponse<TripDto> { Success = false, Message = "Invalid user context. Missing branch." };

                var now = DateTime.UtcNow;

                var trip = await _context.Trips
                    .Include(t => t.Vehicle)
                    .Include(t => t.Driver).ThenInclude(d => d.User)
                    .FirstOrDefaultAsync(t => t.Id == dto.TripId &&
                                              t.CompanyBranchId == _authUser.CompanyBranchId &&
                                              t.IsActive);

                if (trip == null) return new MessageResponse<TripDto> { Success = false, Message = "Trip not found" };

                if (trip.Status != TripStatus.Assigned)
                    return new MessageResponse<TripDto> { Success = false, Message = $"Cannot start trip with status '{trip.Status}'. Trip must be assigned first." };

                if (!trip.DriverId.HasValue)
                    return new MessageResponse<TripDto> { Success = false, Message = "Trip must be assigned to a driver before starting" };

                // Odometer validity
                if (trip.Vehicle != null && trip.Vehicle.Mileage.HasValue && dto.StartOdometer < trip.Vehicle.Mileage.Value)
                {
                    return new MessageResponse<TripDto> { Success = false, Message = $"Start odometer reading ({dto.StartOdometer} km) cannot be less than vehicle's current mileage ({trip.Vehicle.Mileage.Value} km)" };
                }

                // --- GEO CHECK (soft-flag) ---
                const double WARN_THRESHOLD_METERS = 5_000;        // 5 km -> log
                const double SUSPICIOUS_THRESHOLD_METERS = 50_000; // 50 km -> flag as suspicious

                bool locationSuspicious = false;
                string? suspicionNote = null;

                if (dto.Latitude.HasValue && dto.Longitude.HasValue)
                {
                    decimal? refLat = null, refLon = null;
                    bool hasReference = false;

                    // Try trip.OriginLatitude / OriginLongitude (decimal?) if present
                    var tripType = trip.GetType();
                    var originLatProp = tripType.GetProperty("OriginLatitude");
                    var originLonProp = tripType.GetProperty("OriginLongitude");
                    if (originLatProp != null && originLonProp != null)
                    {
                        var oLat = originLatProp.GetValue(trip);
                        var oLon = originLonProp.GetValue(trip);
                        if (oLat != null && oLon != null)
                        {
                            refLat = Convert.ToDecimal(oLat);
                            refLon = Convert.ToDecimal(oLon);
                            hasReference = true;
                        }
                    }

                    // Fallback: vehicle.LastKnownLatitude / LastKnownLongitude (decimal?)
                    if (!hasReference && trip.Vehicle != null)
                    {
                        var vType = trip.Vehicle.GetType();
                        var vLatProp = vType.GetProperty("LastKnownLatitude");
                        var vLonProp = vType.GetProperty("LastKnownLongitude");
                        if (vLatProp != null && vLonProp != null)
                        {
                            var vLat = vLatProp.GetValue(trip.Vehicle);
                            var vLon = vLonProp.GetValue(trip.Vehicle);
                            if (vLat != null && vLon != null)
                            {
                                refLat = Convert.ToDecimal(vLat);
                                refLon = Convert.ToDecimal(vLon);
                                hasReference = true;
                            }
                        }
                    }

                    if (hasReference)
                    {
                        var distMetersNullable = GeoUtils.HaversineDistanceMeters(refLat, refLon, dto.Latitude, dto.Longitude);
                        if (distMetersNullable.HasValue)
                        {
                            var distMeters = distMetersNullable.Value;

                            if (distMeters > WARN_THRESHOLD_METERS)
                            {
                                _logger.LogWarning("Driver {UserId} reported start location {Lat},{Lon} which is {DistanceKm} km from reference for trip {TripId}",
                                    _authUser.UserId, dto.Latitude, dto.Longitude, Math.Round(distMeters / 1000.0, 2), trip.Id);
                            }

                            if (distMeters > SUSPICIOUS_THRESHOLD_METERS)
                            {
                                locationSuspicious = true;
                                suspicionNote = $"SUSPICIOUS LOCATION: reported start {Math.Round(distMeters / 1000.0, 1)} km from reference.";
                            }
                        }
                    }
                    // else: no reference available — accept coords (cannot validate).
                }

                // Apply changes and create checkpoint (soft-flag will be embedded into checkpoint notes)
                using var tx = await _context.Database.BeginTransactionAsync();
                try
                {
                    trip.ActualStartDate = now;
                    trip.StartOdometer = dto.StartOdometer;
                    trip.Status = TripStatus.InProgress;
                    trip.ModifiedDate = now;
                    trip.ModifiedBy = _authUser.UserId;

                    if (trip.Vehicle != null) trip.Vehicle.Mileage = dto.StartOdometer;

                    if (trip.Driver != null)
                    {
                        trip.Driver.ShiftStatus = ShiftStatus.OnDuty;
                        trip.Driver.LastSeen = now;
                    }

                    // Build checkpoint notes
                    var cpNotes = dto.Notes ?? string.Empty;
                    if (dto.LatitudeAccuracy.HasValue)
                    {
                        cpNotes = string.IsNullOrWhiteSpace(cpNotes)
                            ? $"Accuracy: ±{Math.Round((double)dto.LatitudeAccuracy.Value)} m"
                            : $"{cpNotes}\nAccuracy: ±{Math.Round((double)dto.LatitudeAccuracy.Value)} m";
                    }
                    if (locationSuspicious && !string.IsNullOrWhiteSpace(suspicionNote))
                    {
                        cpNotes = string.IsNullOrWhiteSpace(cpNotes)
                            ? suspicionNote
                            : $"{cpNotes}\n\n{suspicionNote}";
                    }

                    var checkpoint = new TripCheckpoint
                    {
                        TripId = trip.Id,
                        Location = trip.Origin,
                        Description = "Trip started",
                        CheckpointTime = now,
                        CheckpointType = CheckpointType.Start,
                        Latitude = dto.Latitude,
                        Longitude = dto.Longitude,
                        Notes = cpNotes,
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

                // background job
                try
                {
                    var correlationId = Guid.NewGuid().ToString();
                    _backgroundJobClient.Enqueue<NotificationWorker>(w => w.ProcessEvent("TripStarted", trip.Id, correlationId));
                }
                catch (Exception bgEx)
                {
                    _logger.LogWarning(bgEx, "Failed to enqueue TripStarted job for trip {TripId}", trip.Id);
                }

                _logger.LogInformation("Trip {TripNumber} started by {UserId}", trip.TripNumber, _authUser.UserId);
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
            EnsureDriverOnly();
            try
            {
                if (_authUser?.CompanyBranchId == null)
                    return new MessageResponse<TripDto> { Success = false, Message = "Invalid user context. Missing branch." };

                var now = DateTime.UtcNow;

                var trip = await _context.Trips
                    .Include(t => t.Vehicle)
                    .Include(t => t.Driver).ThenInclude(d => d.User)
                    .FirstOrDefaultAsync(t => t.Id == dto.TripId &&
                                              t.CompanyBranchId == _authUser.CompanyBranchId &&
                                              t.IsActive);

                if (trip == null) return new MessageResponse<TripDto> { Success = false, Message = "Trip not found" };

                if (trip.Status != TripStatus.InProgress) return new MessageResponse<TripDto> { Success = false, Message = $"Cannot complete trip with status '{trip.Status}'. Trip must be in progress." };

                if (!trip.StartOdometer.HasValue) return new MessageResponse<TripDto> { Success = false, Message = "Trip does not have a start odometer reading" };

                if (dto.EndOdometer <= trip.StartOdometer.Value) return new MessageResponse<TripDto> { Success = false, Message = $"End odometer reading ({dto.EndOdometer} km) must be greater than start odometer ({trip.StartOdometer.Value} km)" };

                // --- GEO CHECK (soft-flag) ---
                const double WARN_THRESHOLD_METERS = 5_000;        // 5 km -> log
                const double SUSPICIOUS_THRESHOLD_METERS = 50_000; // 50 km -> flag as suspicious

                bool locationSuspicious = false;
                string? suspicionNote = null;

                if (dto.Latitude.HasValue && dto.Longitude.HasValue)
                {
                    decimal? refLat = null, refLon = null;
                    bool hasReference = false;

                    // Try trip.DestinationLatitude / DestinationLongitude (decimal?) if present
                    var tripType = trip.GetType();
                    var destLatProp = tripType.GetProperty("DestinationLatitude");
                    var destLonProp = tripType.GetProperty("DestinationLongitude");
                    if (destLatProp != null && destLonProp != null)
                    {
                        var dLat = destLatProp.GetValue(trip);
                        var dLon = destLonProp.GetValue(trip);
                        if (dLat != null && dLon != null)
                        {
                            refLat = Convert.ToDecimal(dLat);
                            refLon = Convert.ToDecimal(dLon);
                            hasReference = true;
                        }
                    }

                    // fallback to vehicle last known
                    if (!hasReference && trip.Vehicle != null)
                    {
                        var vType = trip.Vehicle.GetType();
                        var vLatProp = vType.GetProperty("LastKnownLatitude");
                        var vLonProp = vType.GetProperty("LastKnownLongitude");
                        if (vLatProp != null && vLonProp != null)
                        {
                            var vLat = vLatProp.GetValue(trip.Vehicle);
                            var vLon = vLonProp.GetValue(trip.Vehicle);
                            if (vLat != null && vLon != null)
                            {
                                refLat = Convert.ToDecimal(vLat);
                                refLon = Convert.ToDecimal(vLon);
                                hasReference = true;
                            }
                        }
                    }

                    if (hasReference)
                    {
                        var distMetersNullable = GeoUtils.HaversineDistanceMeters(refLat, refLon, dto.Latitude, dto.Longitude);
                        if (distMetersNullable.HasValue)
                        {
                            var distMeters = distMetersNullable.Value;

                            if (distMeters > WARN_THRESHOLD_METERS)
                            {
                                _logger.LogWarning("Driver {UserId} reported completion location {Lat},{Lon} which is {DistanceKm} km from reference for trip {TripId}",
                                    _authUser.UserId, dto.Latitude, dto.Longitude, Math.Round(distMeters / 1000.0, 2), trip.Id);
                            }

                            if (distMeters > SUSPICIOUS_THRESHOLD_METERS)
                            {
                                locationSuspicious = true;
                                suspicionNote = $"SUSPICIOUS LOCATION: reported completion {Math.Round(distMeters / 1000.0, 1)} km from reference.";
                            }
                        }
                    }
                }

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

                    if (trip.Vehicle != null) trip.Vehicle.Mileage = dto.EndOdometer;

                    if (trip.Driver != null)
                    {
                        trip.Driver.ShiftStatus = ShiftStatus.Available;
                        trip.Driver.LastSeen = now;
                    }

                    // checkpoint notes
                    var cpNotes = dto.Notes ?? string.Empty;
                    if (dto.LatitudeAccuracy.HasValue)
                    {
                        cpNotes = string.IsNullOrWhiteSpace(cpNotes)
                            ? $"Accuracy: ±{Math.Round((double)dto.LatitudeAccuracy.Value)} m"
                            : $"{cpNotes}\nAccuracy: ±{Math.Round((double)dto.LatitudeAccuracy.Value)} m";
                    }
                    if (locationSuspicious && !string.IsNullOrWhiteSpace(suspicionNote))
                    {
                        cpNotes = string.IsNullOrWhiteSpace(cpNotes)
                            ? suspicionNote
                            : $"{cpNotes}\n\n{suspicionNote}";
                    }

                    var checkpoint = new TripCheckpoint
                    {
                        TripId = trip.Id,
                        Location = trip.Destination,
                        Description = "Trip completed",
                        CheckpointTime = now,
                        CheckpointType = CheckpointType.End,
                        Latitude = dto.Latitude,
                        Longitude = dto.Longitude,
                        Notes = cpNotes,
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

                // background job
                try
                {
                    var correlationId = Guid.NewGuid().ToString();
                    _backgroundJobClient.Enqueue<NotificationWorker>(w => w.ProcessEvent("TripCompleted", trip.Id, correlationId));
                }
                catch (Exception bgEx)
                {
                    _logger.LogWarning(bgEx, "Failed to enqueue TripCompleted job for trip {TripId}", trip.Id);
                }

                _logger.LogInformation("Trip {TripNumber} completed by {UserId} - Distance: {Distance}", trip.TripNumber, _authUser.UserId, trip.ActualDistance);
                var result = await GetTripByIdAsync(trip.Id);
                return new MessageResponse<TripDto> { Success = true, Message = $"Trip completed successfully. Distance covered: {trip.ActualDistance} km", Result = result.Result };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing trip");
                return new MessageResponse<TripDto> { Success = false, Message = "An error occurred while completing the trip" };
            }
        }

        //public async Task<MessageResponse<TripDto>> CompleteTripAsync(CompleteTripDto dto)
        //{
        //    EnsureDriverOnly();
        //    try
        //    {
        //        if (_authUser?.CompanyBranchId == null) return new MessageResponse<TripDto> { Success = false, Message = "Invalid user context. Missing branch." };
        //        var now = DateTime.UtcNow;

        //        var trip = await _context.Trips
        //            .Include(t => t.Vehicle)
        //            .Include(t => t.Driver).ThenInclude(d => d.User)
        //            .FirstOrDefaultAsync(t => t.Id == dto.TripId &&
        //                                      t.CompanyBranchId == _authUser.CompanyBranchId &&
        //                                      t.IsActive);

        //        if (trip == null) return new MessageResponse<TripDto> { Success = false, Message = "Trip not found" };

        //        if (trip.Status != TripStatus.InProgress) return new MessageResponse<TripDto> { Success = false, Message = $"Cannot complete trip with status '{trip.Status}'. Trip must be in progress." };

        //        if (!trip.StartOdometer.HasValue) return new MessageResponse<TripDto> { Success = false, Message = "Trip does not have a start odometer reading" };

        //        if (dto.EndOdometer <= trip.StartOdometer.Value) return new MessageResponse<TripDto> { Success = false, Message = $"End odometer reading ({dto.EndOdometer} km) must be greater than start odometer ({trip.StartOdometer.Value} km)" };

        //        using var tx = await _context.Database.BeginTransactionAsync();
        //        try
        //        {
        //            trip.ActualEndDate = now;
        //            trip.EndOdometer = dto.EndOdometer;
        //            trip.ActualDistance = dto.EndOdometer - trip.StartOdometer.Value;
        //            trip.ActualFuelCost = dto.ActualFuelCost;
        //            trip.Status = TripStatus.Completed;
        //            trip.ModifiedDate = now;
        //            trip.ModifiedBy = _authUser.UserId;

        //            if (trip.Vehicle != null) trip.Vehicle.Mileage = dto.EndOdometer;

        //            if (trip.Driver != null)
        //            {
        //                trip.Driver.ShiftStatus = ShiftStatus.Available;
        //                trip.Driver.LastSeen = now;
        //            }

        //            var checkpoint = new TripCheckpoint
        //            {
        //                TripId = trip.Id,
        //                Location = trip.Destination,
        //                Description = "Trip completed",
        //                CheckpointTime = now,
        //                CheckpointType = CheckpointType.End,
        //                Latitude = dto.Latitude,
        //                Longitude = dto.Longitude,
        //                Notes = dto.Notes,
        //                IsActive = true,
        //                CreatedDate = now,
        //                CreatedBy = _authUser.UserId
        //            };

        //            _context.TripCheckpoints.Add(checkpoint);
        //            await _context.SaveChangesAsync();
        //            await tx.CommitAsync();
        //        }
        //        catch
        //        {
        //            await tx.RollbackAsync();
        //            throw;
        //        }

        //        // Enqueue TripCompleted for background processing (notifications + webhook)
        //        try
        //        {
        //            var correlationId = Guid.NewGuid().ToString();
        //            _backgroundJobClient.Enqueue<NotificationWorker>(w => w.ProcessEvent("TripCompleted", trip.Id, correlationId));
        //        }
        //        catch (Exception bgEx)
        //        {
        //            _logger.LogWarning(bgEx, "Failed to enqueue TripCompleted job for trip {TripId}", trip.Id);
        //        }

        //        _logger.LogInformation("Trip {TripNumber} completed by {UserId} - Distance: {Distance}", trip.TripNumber, _authUser.UserId, trip.ActualDistance);
        //        var result = await GetTripByIdAsync(trip.Id);
        //        return new MessageResponse<TripDto> { Success = true, Message = $"Trip completed successfully. Distance covered: {trip.ActualDistance} km", Result = result.Result };
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error completing trip");
        //        return new MessageResponse<TripDto> { Success = false, Message = "An error occurred while completing the trip" };
        //    }
        //}

        public async Task<MessageResponse<TripDto>> CancelTripAsync(CancelTripDto dto)
        {
            try
            {
                if (_authUser?.CompanyBranchId == null) return new MessageResponse<TripDto> { Success = false, Message = "Invalid user context. Missing branch." };
                var now = DateTime.UtcNow;

                var trip = await _context.Trips
                    .Include(t => t.Driver).ThenInclude(d => d.User)
                    .FirstOrDefaultAsync(t => t.Id == dto.TripId &&
                                              t.CompanyBranchId == _authUser.CompanyBranchId &&
                                              t.IsActive);

                if (trip == null) return new MessageResponse<TripDto> { Success = false, Message = "Trip not found" };

                if (trip.Status == TripStatus.Completed) return new MessageResponse<TripDto> { Success = false, Message = "Cannot cancel a completed trip" };

                trip.Status = TripStatus.Cancelled;
                trip.CancellationReason = dto.CancellationReason;
                trip.CancellationDate = now;
                trip.ModifiedDate = now;
                trip.ModifiedBy = _authUser.UserId;

                await _context.SaveChangesAsync();

                // Enqueue TripCancelled notifications
                try
                {
                    var correlationId = Guid.NewGuid().ToString();
                    _backgroundJobClient.Enqueue<NotificationWorker>(w => w.ProcessEvent("TripCancelled", trip.Id, correlationId));
                }
                catch (Exception bgEx)
                {
                    _logger.LogWarning(bgEx, "Failed to enqueue TripCancelled job for trip {TripId}", trip.Id);
                }

                _logger.LogInformation("Trip {TripNumber} cancelled by {UserId}", trip.TripNumber, _authUser.UserId);
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
                if (_authUser?.CompanyBranchId == null) return new MessageResponse<TripDto> { Success = false, Message = "Invalid user context. Missing branch." };
                var now = DateTime.UtcNow;

                var trip = await _context.Trips
                    .Include(t => t.Driver).ThenInclude(d => d.User)
                    .FirstOrDefaultAsync(t => t.Id == dto.TripId &&
                                              t.CompanyBranchId == _authUser.CompanyBranchId &&
                                              t.IsActive);

                if (trip == null) return new MessageResponse<TripDto> { Success = false, Message = "Trip not found" };

                if (!trip.RequiresApproval) return new MessageResponse<TripDto> { Success = false, Message = "This trip does not require approval" };

                if (trip.Status != TripStatus.PendingApproval) return new MessageResponse<TripDto> { Success = false, Message = $"Cannot approve trip with status '{trip.Status}'" };

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
                            trip.Notes = string.IsNullOrWhiteSpace(trip.Notes) ? $"Approval Comments: {dto.Comments}" : $"{trip.Notes}\n\nApproval Comments: {dto.Comments}";
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

                // Enqueue TripApproved event for notifications
                try
                {
                    var correlationId = Guid.NewGuid().ToString();
                    _backgroundJobClient.Enqueue<NotificationWorker>(w => w.ProcessEvent("TripApproved", trip.Id, correlationId));
                }
                catch (Exception bgEx)
                {
                    _logger.LogWarning(bgEx, "Failed to enqueue TripApproved job for trip {TripId}", trip.Id);
                }

                var action = dto.IsApproved ? "approved" : "rejected";
                _logger.LogInformation("Trip {TripNumber} {Action} by {UserId}", trip.TripNumber, action, _authUser.UserId);
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
                if (_authUser?.CompanyBranchId == null) return new MessageResponse<TripExpenseDto> { Success = false, Message = "Invalid user context. Missing branch." };

                var trip = await _context.Trips
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Id == dto.TripId &&
                                              t.CompanyBranchId == _authUser.CompanyBranchId &&
                                              t.IsActive);

                if (trip == null) return new MessageResponse<TripExpenseDto> { Success = false, Message = "Trip not found" };

                if (dto.Amount <= 0) return new MessageResponse<TripExpenseDto> { Success = false, Message = "Expense amount must be greater than zero" };

                var now = DateTime.UtcNow;

                var expense = new TripExpense
                {
                    TripId = dto.TripId,
                    ExpenseType = dto.ExpenseType,
                    Description = dto.Description,
                    Amount = dto.Amount,
                    ExpenseDate = dto.ExpenseDate,
                    ReceiptFileName = dto.ReceiptFileName,
                    IsVerified = false,
                    IsActive = true,
                    CreatedDate = now,
                    CreatedBy = _authUser.UserId
                };

                _context.TripExpenses.Add(expense);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Expense {ExpenseId} added to trip {TripNumber} by {UserId}", expense.Id, trip.TripNumber, _authUser.UserId);

                // Enqueue background job to notify admins/finance
                try
                {
                    var correlationId = Guid.NewGuid().ToString();
                    _backgroundJobClient.Enqueue<NotificationWorker>(w => w.ProcessEvent("TripExpenseAdded", trip.Id, correlationId));
                }
                catch (Exception bgEx)
                {
                    _logger.LogWarning(bgEx, "Failed to enqueue TripExpenseAdded job for trip {TripId}", trip.Id);
                }

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
                if (_authUser?.CompanyBranchId == null) return new MessageResponse<List<TripExpenseDto>> { Success = false, Message = "Invalid user context. Missing branch." };

                var trip = await _context.Trips.AsNoTracking()
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
                if (_authUser?.CompanyBranchId == null) return new MessageResponse { Success = false, Message = "Invalid user context. Missing branch." };

                var expense = await _context.TripExpenses
                    .Include(e => e.Trip)
                    .FirstOrDefaultAsync(e => e.Id == expenseId &&
                                              e.Trip.CompanyBranchId == _authUser.CompanyBranchId &&
                                              e.IsActive);

                if (expense == null) return new MessageResponse { Success = false, Message = "Expense not found" };

                expense.IsActive = false;
                expense.ModifiedDate = DateTime.UtcNow;
                expense.ModifiedBy = _authUser.UserId;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Expense {ExpenseId} deleted from trip {TripId} by {UserId}", expenseId, expense.TripId, _authUser.UserId);
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
                if (_authUser?.CompanyBranchId == null) return new MessageResponse<TripExpenseDto> { Success = false, Message = "Invalid user context. Missing branch." };

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

                _logger.LogInformation("Expense {ExpenseId} verified for trip {TripId} by {UserId}", expenseId, expense.TripId, _authUser.UserId);

                var dto = new TripExpenseDto
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

                return new MessageResponse<TripExpenseDto> { Success = true, Message = "Expense verified successfully", Result = dto };
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
                if (_authUser?.CompanyBranchId == null) return new MessageResponse<TripStatistics> { Success = false, Message = "Invalid user context. Missing branch." };

                var query = _context.Trips
                    .AsNoTracking()
                    .Where(t => t.CompanyBranchId == _authUser.CompanyBranchId && t.IsActive);

                if (startDate.HasValue) query = query.Where(t => t.CreatedDate >= startDate.Value);
                if (endDate.HasValue) query = query.Where(t => t.CreatedDate <= endDate.Value);

                var trips = await query.ToListAsync();

                var now = DateTime.UtcNow;
                var weekStart = now.AddDays(-(int)now.DayOfWeek);
                var monthStart = new DateTime(now.Year, now.Month, 1);

                var statistics = new TripStatistics
                {
                    TotalTrips = trips.Count,
                    ScheduledTrips = trips.Count(t => t.Status == TripStatus.Scheduled || t.Status == TripStatus.Assigned),
                    ActiveTrips = trips.Count(t => t.Status == TripStatus.InProgress),
                    CompletedTrips = trips.Count(t => t.Status == TripStatus.Completed),
                    CancelledTrips = trips.Count(t => t.Status == TripStatus.Cancelled),
                    PendingApprovalTrips = trips.Count(t => t.Status == TripStatus.PendingApproval),
                    TotalDistanceCovered = trips.Where(t => t.ActualDistance.HasValue).Sum(t => t.ActualDistance.Value),
                    TotalFuelCost = trips.Where(t => t.ActualFuelCost.HasValue).Sum(t => t.ActualFuelCost.Value),
                    TripsThisWeek = trips.Count(t => t.CreatedDate >= weekStart),
                    TripsThisMonth = trips.Count(t => t.CreatedDate >= monthStart)
                };

                var completedTrips = trips.Where(t => t.Status == TripStatus.Completed && t.ActualDistance.HasValue).ToList();
                if (completedTrips.Any())
                {
                    statistics.AverageTripDistance = completedTrips.Average(t => t.ActualDistance.Value);
                    var tripsWithCost = completedTrips.Where(t => t.ActualFuelCost.HasValue).ToList();
                    if (tripsWithCost.Any()) statistics.AverageTripCost = tripsWithCost.Average(t => t.ActualFuelCost.Value);
                }

                return new MessageResponse<TripStatistics> { Success = true, Result = statistics };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving trip statistics");
                return new MessageResponse<TripStatistics> { Success = false, Message = "An error occurred while retrieving statistics" };
            }
        }

        public async Task<MessageResponse<DashboardSeriesDto>> GetDashboardSeriesAsync()
        {
            try
            {
                if (_authUser?.CompanyBranchId == null)
                    return new MessageResponse<DashboardSeriesDto> { Success = false, Message = "Invalid user context. Missing branch." };

                var branchId = _authUser.CompanyBranchId.Value;
                var now = DateTime.UtcNow;
                var fromDate = now.Date.AddDays(-6); // last 7 days (inclusive)

                // Get relevant trips for the branch and timeframe
                var baseQuery = _context.Trips
                    .AsNoTracking()
                    .Where(t => t.CompanyBranchId == branchId && t.IsActive);

                // === Status counts (group by status) ===
                var statusGroups = await baseQuery
                    .GroupBy(t => t.Status)
                    .Select(g => new { Status = g.Key, Count = g.Count() })
                    .ToListAsync();

                var statusCounts = new Dictionary<string, int>();
                foreach (var g in statusGroups)
                {
                    statusCounts[g.Status.ToString()] = g.Count;
                }

                // === 7-day trend (created date) ===
                // Fetch relevant recent trips and group in-memory by date (safe for 7 days)
                var recentTrips = await baseQuery
                    .Where(t => t.CreatedDate >= fromDate)
                    .Select(t => new { t.CreatedDate })
                    .ToListAsync();

                var trendDict = recentTrips
                    .GroupBy(t => t.CreatedDate.Date)
                    .ToDictionary(g => g.Key, g => g.Count());

                var trendList = new List<DailySeriesPoint>();
                for (var day = fromDate; day <= now.Date; day = day.AddDays(1))
                {
                    trendDict.TryGetValue(day, out var c);
                    trendList.Add(new DailySeriesPoint { Date = day, Count = c });
                }

                var dto = new DashboardSeriesDto
                {
                    StatusCounts = statusCounts,
                    SevenDayTrend = trendList
                };

                return new MessageResponse<DashboardSeriesDto> { Success = true, Result = dto };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error building dashboard series");
                return new MessageResponse<DashboardSeriesDto> { Success = false, Message = "An error occurred while retrieving dashboard series" };
            }
        }


        public async Task<MessageResponse<TripDashboardViewModel>> GetDashboardDataAsync()
        {
            try
            {
                if (_authUser?.CompanyBranchId == null) return new MessageResponse<TripDashboardViewModel> { Success = false, Message = "Invalid user context. Missing branch." };

                var statisticsResponse = await GetTripStatisticsAsync(null, null);
                if (!statisticsResponse.Success) return new MessageResponse<TripDashboardViewModel> { Success = false, Message = statisticsResponse.Message };

                var now = DateTime.UtcNow;

                var upcomingTrips = await _context.Trips
                    .AsNoTracking()
                    .Include(t => t.Vehicle).ThenInclude(v => v.VehicleMake)
                    .Include(t => t.Vehicle).ThenInclude(v => v.VehicleModel)
                    .Include(t => t.Driver).ThenInclude(d => d.User)
                    .Where(t => t.CompanyBranchId == _authUser.CompanyBranchId &&
                                t.IsActive &&
                                (t.Status == TripStatus.Scheduled || t.Status == TripStatus.Assigned) &&
                                t.ScheduledStartDate >= now &&
                                t.ScheduledStartDate <= now.AddDays(7))
                    .OrderBy(t => t.ScheduledStartDate)
                    .Take(10)
                    .Select(t => new TripListDto
                    {
                        Id = t.Id,
                        TripNumber = t.TripNumber,
                        VehiclePlateNo = t.Vehicle.VehicleMake.Name + " " + t.Vehicle.VehicleModel.Name + " " + t.Vehicle.PlateNo,
                        DriverName = t.Driver != null ? t.Driver.User.FirstName + " " + t.Driver.User.LastName : null,
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
                    .Include(t => t.Vehicle).ThenInclude(v => v.VehicleMake)
                    .Include(t => t.Vehicle).ThenInclude(v => v.VehicleModel)
                    .Include(t => t.Driver).ThenInclude(d => d.User)
                    .Where(t => t.CompanyBranchId == _authUser.CompanyBranchId &&
                                t.IsActive &&
                                t.Status == TripStatus.InProgress)
                    .OrderByDescending(t => t.ActualStartDate)
                    .Take(10)
                    .Select(t => new TripListDto
                    {
                        Id = t.Id,
                        TripNumber = t.TripNumber,
                        VehiclePlateNo = t.Vehicle.VehicleMake.Name + " " + t.Vehicle.VehicleModel.Name + " " + t.Vehicle.PlateNo,
                        DriverName = t.Driver != null ? t.Driver.User.FirstName + " " + t.Driver.User.LastName : null,
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
                    .Include(t => t.Vehicle).ThenInclude(v => v.VehicleMake)
                    .Include(t => t.Vehicle).ThenInclude(v => v.VehicleModel)
                    .Include(t => t.Driver).ThenInclude(d => d.User)
                    .Where(t => t.CompanyBranchId == _authUser.CompanyBranchId &&
                                t.IsActive &&
                                t.Status == TripStatus.PendingApproval)
                    .OrderByDescending(t => t.CreatedDate)
                    .Take(10)
                    .Select(t => new TripListDto
                    {
                        Id = t.Id,
                        TripNumber = t.TripNumber,
                        VehiclePlateNo = t.Vehicle.VehicleMake.Name + " " + t.Vehicle.VehicleModel.Name + " " + t.Vehicle.PlateNo,
                        DriverName = t.Driver != null ? t.Driver.User.FirstName + " " + t.Driver.User.LastName : null,
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

                var seriesResp = await GetDashboardSeriesAsync();
                if (seriesResp.Success && seriesResp.Result != null)
                {
                    dashboard.StatusCounts = seriesResp.Result.StatusCounts ?? new Dictionary<string, int>();
                    dashboard.SevenDayTrend = seriesResp.Result.SevenDayTrend ?? new List<DailySeriesPoint>();
                }
                else
                {
                    // If helper fails, default to empty collections (non-fatal)
                    dashboard.StatusCounts = new Dictionary<string, int>();
                    dashboard.SevenDayTrend = new List<DailySeriesPoint>();
                }

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
                // Verify driver belongs to branch
                var driver = await _context.Drivers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(d => d.Id == driverId &&
                                              d.CompanyBranchId == _authUser.CompanyBranchId &&
                                              d.IsActive);

                if (driver == null) return new MessageResponse<List<TripListDto>> { Success = false, Message = "Driver not found" };

                var trips = await _context.Trips
                    .AsNoTracking()
                    .Include(t => t.Vehicle).ThenInclude(v => v.VehicleMake)
                    .Include(t => t.Vehicle).ThenInclude(v => v.VehicleModel)
                    .Include(t => t.Driver).ThenInclude(d => d.User)
                    .Where(t => t.DriverId == driverId && t.IsActive)
                    .OrderByDescending(t => t.ScheduledStartDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(t => new TripListDto
                    {
                        Id = t.Id,
                        TripNumber = t.TripNumber,
                        VehiclePlateNo = t.Vehicle.VehicleMake.Name + " " + t.Vehicle.VehicleModel.Name + " " + t.Vehicle.PlateNo,
                        DriverName = t.Driver.User.FirstName + " " + t.Driver.User.LastName,
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
                var vehicle = await _context.Vehicles
                    .AsNoTracking()
                    .FirstOrDefaultAsync(v => v.Id == vehicleId &&
                                              v.CompanyBranchId == _authUser.CompanyBranchId &&
                                              v.IsActive);

                if (vehicle == null) return new MessageResponse<List<TripListDto>> { Success = false, Message = "Vehicle not found" };

                var trips = await _context.Trips
                    .AsNoTracking()
                    .Include(t => t.Vehicle)
                    .Include(t => t.Driver).ThenInclude(d => d.User)
                    .Where(t => t.VehicleId == vehicleId && t.IsActive)
                    .OrderByDescending(t => t.ScheduledStartDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(t => new TripListDto
                    {
                        Id = t.Id,
                        TripNumber = t.TripNumber,
                        VehiclePlateNo = t.Vehicle.PlateNo,
                        DriverName = t.Driver != null ? t.Driver.User.FirstName + " " + t.Driver.User.LastName : null,
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
                if (_authUser?.CompanyBranchId == null) return new MessageResponse<bool> { Success = false, Message = "Invalid user context. Missing branch.", Result = false };

                // Vehicle conflicts
                var vehicleConflictQuery = _context.Trips
                    .AsNoTracking()
                    .Where(t => t.VehicleId == vehicleId &&
                                t.CompanyBranchId == _authUser.CompanyBranchId &&
                                t.IsActive &&
                                (t.Status == TripStatus.Scheduled || t.Status == TripStatus.Assigned || t.Status == TripStatus.Approved || t.Status == TripStatus.InProgress) &&
                                (t.ScheduledStartDate < endDate && t.ScheduledEndDate > startDate));

                if (excludeTripId.HasValue) vehicleConflictQuery = vehicleConflictQuery.Where(t => t.Id != excludeTripId.Value);

                var hasVehicleConflict = await vehicleConflictQuery.AnyAsync();
                if (hasVehicleConflict) return new MessageResponse<bool> { Success = false, Message = "Vehicle is already assigned to another trip during this period", Result = false };

                if (driverId.HasValue)
                {
                    var driverConflictQuery = _context.Trips
                        .AsNoTracking()
                        .Where(t => t.DriverId == driverId.Value &&
                                    t.CompanyBranchId == _authUser.CompanyBranchId &&
                                    t.IsActive &&
                                    (t.Status == TripStatus.Scheduled || t.Status == TripStatus.Assigned || t.Status == TripStatus.Approved || t.Status == TripStatus.InProgress) &&
                                    (t.ScheduledStartDate < endDate && t.ScheduledEndDate > startDate));

                    if (excludeTripId.HasValue) driverConflictQuery = driverConflictQuery.Where(t => t.Id != excludeTripId.Value);

                    var hasDriverConflict = await driverConflictQuery.AnyAsync();
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

        /// <summary>
        /// Returns the active drivers belonging to the current user's branch.
        /// </summary>
        public async Task<MessageResponse<List<SimpleDriverDto>>> GetDriversForBranchAsync()
        {
            try
            {
                if (!_authUser.CompanyBranchId.HasValue)
                {
                    return new MessageResponse<List<SimpleDriverDto>>
                    {
                        Success = false,
                        Message = "Invalid user context. Missing branch."
                    };
                }

                var branchId = _authUser.CompanyBranchId.Value;

                var drivers = await _context.Drivers
                    .AsNoTracking()
                    .Include(d => d.User)
                    .Where(d => d.CompanyBranchId == branchId && d.IsActive)
                    .OrderBy(d => d.User.FirstName)
                    .Select(d => new SimpleDriverDto
                    {
                        Id = d.Id,
                        IdentityUserId = d.User != null ? d.User.Id : null,
                        FullName = d.User != null ? (d.User.FirstName + " " + d.User.LastName) : "Unknown"
                    })
                    .ToListAsync();

                return new MessageResponse<List<SimpleDriverDto>>
                {
                    Success = true,
                    Result = drivers
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving drivers for branch");
                return new MessageResponse<List<SimpleDriverDto>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving drivers"
                };
            }
        }

        /// <summary>
        /// Returns vehicles currently assigned to the supplied driver (DriverVehicle active record).
        /// If scheduledStart/scheduledEnd are provided, the method ensures the assignment covers the requested window.
        /// Optionally excludes vehicles that have overlapping trips during the requested window (excludeVehiclesOnTripOverlap = true).
        /// </summary>
        public async Task<MessageResponse<List<SimpleVehicleDto>>> GetVehiclesForDriverAsync(long driverId, DateTime? scheduledStart = null, DateTime? scheduledEnd = null, bool excludeVehiclesOnTripOverlap = true)
        {
            try
            {
                if (!_authUser.CompanyBranchId.HasValue)
                {
                    return new MessageResponse<List<SimpleVehicleDto>>
                    {
                        Success = false,
                        Message = "Invalid user context. Missing branch."
                    };
                }

                var branchId = _authUser.CompanyBranchId.Value; // <- use .Value consistently

                // Verify driver belongs to branch
                var driver = await _context.Drivers
                    .AsNoTracking()
                    .Include(d => d.User)
                    .FirstOrDefaultAsync(d => d.Id == driverId && d.CompanyBranchId == branchId && d.IsActive);

                if (driver == null)
                {
                    return new MessageResponse<List<SimpleVehicleDto>>
                    {
                        Success = false,
                        Message = "Driver not found"
                    };
                }

                var now = DateTime.UtcNow;
                var windowStart = scheduledStart ?? now;
                var windowEnd = scheduledEnd ?? scheduledStart ?? now;

                var dvQuery = _context.DriverVehicles
                    .AsNoTracking()
                    .Include(dv => dv.Vehicle)
                        .ThenInclude(v => v.VehicleMake)
                    .Include(dv => dv.Vehicle)
                        .ThenInclude(v => v.VehicleModel)
                    .Where(dv => dv.DriverId == driverId);

                // assignment must cover requested window
                dvQuery = dvQuery.Where(dv =>
                    (dv.StartDate == null || dv.StartDate <= windowEnd) &&
                    (dv.EndDate == null || dv.EndDate >= windowStart)
                );

                // ensure the vehicle belongs to the same branch (use branchId)
                dvQuery = dvQuery.Where(dv => dv.Vehicle != null && dv.Vehicle.CompanyBranchId == branchId);

                var candidateVehicles = await dvQuery
                    .Select(dv => dv.Vehicle!)
                    .Distinct()
                    .ToListAsync();

                if (excludeVehiclesOnTripOverlap && candidateVehicles.Any() && (scheduledStart.HasValue || scheduledEnd.HasValue))
                {
                    var s = windowStart;
                    var e = windowEnd;

                    var conflictingVehicleIds = await _context.Trips
                        .AsNoTracking()
                        .Where(t =>
                            t.IsActive &&
                            (t.Status == TripStatus.Scheduled || t.Status == TripStatus.Assigned || t.Status == TripStatus.InProgress || t.Status == TripStatus.Approved) &&
                            t.VehicleId != null &&
                            (t.ScheduledStartDate <= e && t.ScheduledEndDate >= s)
                        )
                        .Select(t => t.VehicleId)
                        .Distinct()
                        .ToListAsync();

                    candidateVehicles = candidateVehicles
                        .Where(v => !conflictingVehicleIds.Contains(v.Id))
                        .ToList();
                }

                var vehicles = candidateVehicles
                    .Select(v => new SimpleVehicleDto
                    {
                        Id = v.Id,
                        PlateNo = v.PlateNo,
                        Make = v.VehicleMake != null ? v.VehicleMake.Name : null,
                        Model = v.VehicleModel != null ? v.VehicleModel.Name : null
                    })
                    .OrderBy(v => v.PlateNo)
                    .ToList();

                return new MessageResponse<List<SimpleVehicleDto>> { Success = true, Result = vehicles };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving vehicles for driver {DriverId}", driverId);
                return new MessageResponse<List<SimpleVehicleDto>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving vehicles for the driver"
                };
            }
        }


        private async Task<string> GenerateTripNumberAsync()
        {
            var date = DateTime.UtcNow;
            var prefix = $"TRP{date:yyyyMMdd}";

            var lastTrip = await _context.Trips
                .AsNoTracking()
                .Where(t => t.TripNumber.StartsWith(prefix))
                .OrderByDescending(t => t.TripNumber)
                .FirstOrDefaultAsync();

            int sequence = 1;
            if (lastTrip != null && lastTrip.TripNumber.Length > prefix.Length)
            {
                var lastSequence = lastTrip.TripNumber.Substring(prefix.Length);
                if (int.TryParse(lastSequence, out int lastNumber))
                {
                    sequence = lastNumber + 1;
                }
            }

            return $"{prefix}{sequence:D4}";
        }

        private TripDto MapTripToDto(Trip trip)
        {
            var tripDto = new TripDto
            {
                Id = trip.Id,
                TripNumber = trip.TripNumber,
                CompanyBranchId = trip.CompanyBranchId,
                CompanyId = trip.CompanyId,
                VehicleId = trip.VehicleId,
                VehiclePlateNo = trip.Vehicle?.PlateNo,
                VehicleMake = trip.Vehicle?.VehicleMake?.Name,
                VehicleModel = trip.Vehicle?.VehicleModel?.Name,
                VehicleMileage = trip.Vehicle?.Mileage,
                DriverId = trip.DriverId,
                DriverName = trip.Driver != null ? $"{trip.Driver.User.FirstName} {trip.Driver.User.LastName}" : null,
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

            // New: detect suspicious checkpoints (case-insensitive)
            try
            {
                tripDto.HasSuspiciousLocation =
                    trip.TripCheckpoints?.Any(c =>
                        !string.IsNullOrWhiteSpace(c.Notes) &&
                        c.Notes.IndexOf("SUSPICIOUS LOCATION", StringComparison.OrdinalIgnoreCase) >= 0
                    ) ?? false;
            }
            catch
            {
                // Defensive: if TripCheckpoints wasn't loaded or anything goes wrong, default to false
                tripDto.HasSuspiciousLocation = false;
            }

            return tripDto;
        }


        #endregion
    }

}