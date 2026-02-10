using FleetManager.Business.DataObjects.ApiModels;
using FleetManager.Business.DataObjects.TripsDto;
using FleetManager.Business.Enums;
using FleetManager.Business.Interfaces.DriverVehicleModule;
using FleetManager.Business.Interfaces.TripLocationModule;
using FleetManager.Business.Interfaces.TripModule;
using FleetManager.Business.Interfaces.UserModule;
using FleetManager.Business.UtilityModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FleetManager.App.Controllers
{
    [Route("api/driver/trips")]
    [ApiController]
    [Authorize(Policy = "DriverApi")]
    public class DriverTripsApiController : ControllerBase
    {
        private readonly ITripService _tripService;
        private readonly IDriverVehicleService _assignmentService;
        private readonly ITripLocationService _locationService;
        private readonly ILogger<DriverTripsApiController> _logger;
        private readonly IAuthUser _authUser;

        public DriverTripsApiController(
            ITripService tripService,
            IDriverVehicleService assignmentService,
            ITripLocationService locationService,
            ILogger<DriverTripsApiController> logger,
            IAuthUser authUser)
        {
            _tripService = tripService;
            _assignmentService = assignmentService;
            _locationService = locationService;
            _logger = logger;
            _authUser = authUser;
        }

        /// <summary>
        /// Get all trips assigned to the current driver with optional filtering
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<PaginatedResult<TripListResponse>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTrips([FromQuery] TripFilterRequest filter)
        {
            try
            {
                var userId = _authUser.UserId;
                var driverId = await _assignmentService.GetDriverIdByUserAsync(userId);

                if (driverId == 0)
                {
                    return Ok(new ApiResponse<PaginatedResult<TripListResponse>>
                    {
                        Success = false,
                        Message = "Driver profile not found"
                    });
                }

                // Build filter DTO
                var filterDto = new TripFilterDto
                {
                    DriverId = driverId,
                    Status = !string.IsNullOrEmpty(filter.Status) && Enum.TryParse<TripStatus>(filter.Status, out var status)
                        ? status
                        : null,
                    StartDate = filter.StartDate,
                    EndDate = filter.EndDate,
                    Page = filter.Page,
                    PageSize = Math.Min(filter.PageSize, 100) // Cap at 100
                };

                var response = await _tripService.GetTripsAsync(filterDto);

                if (!response.Success)
                {
                    return Ok(new ApiResponse<PaginatedResult<TripListResponse>>
                    {
                        Success = false,
                        Message = response.Message
                    });
                }

                // Map to API response
                var apiResult = new PaginatedResult<TripListResponse>
                {
                    Items = response.Result.Items.Select(t => new TripListResponse
                    {
                        TripId = t.Id,
                        TripNumber = t.TripNumber,
                        VehiclePlateNo = t.VehiclePlateNo,
                        Origin = t.Origin,
                        Destination = t.Destination,
                        ScheduledStartDate = t.ScheduledStartDate,
                        ScheduledEndDate = t.ScheduledEndDate,
                        Status = t.Status.ToString(),
                        Priority = t.Priority.ToString(),
                        RequiresApproval = t.RequiresApproval
                    }).ToList(),
                    Page = response.Result.Page,
                    PageSize = response.Result.PageSize,
                    TotalCount = response.Result.TotalCount
                };

                return Ok(new ApiResponse<PaginatedResult<TripListResponse>>
                {
                    Success = true,
                    Message = $"Found {apiResult.TotalCount} trip(s)",
                    Data = apiResult
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving trips");
                return StatusCode(500, new ApiResponse<PaginatedResult<TripListResponse>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving trips"
                });
            }
        }

        /// <summary>
        /// Get current active trip for the driver (if any)
        /// </summary>
        [HttpGet("current")]
        [ProducesResponseType(typeof(ApiResponse<TripResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCurrentTrip()
        {
            try
            {
                var userId = _authUser.UserId;
                var driverId = await _assignmentService.GetDriverIdByUserAsync(userId);

                var filterDto = new TripFilterDto
                {
                    DriverId = driverId,
                    Status = TripStatus.InProgress,
                    Page = 1,
                    PageSize = 1
                };

                var response = await _tripService.GetTripsAsync(filterDto);

                if (!response.Success || !response.Result.Items.Any())
                {
                    return Ok(new ApiResponse<TripResponse>
                    {
                        Success = false,
                        Message = "No active trip found",
                        Data = null
                    });
                }

                var trip = response.Result.Items.First();
                var tripDetails = await _tripService.GetTripByIdAsync(trip.Id);

                if (!tripDetails.Success)
                {
                    return Ok(new ApiResponse<TripResponse>
                    {
                        Success = false,
                        Message = "Error loading trip details"
                    });
                }

                return Ok(new ApiResponse<TripResponse>
                {
                    Success = true,
                    Message = "Current trip retrieved",
                    Data = MapToTripResponse(tripDetails.Result)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving current trip");
                return StatusCode(500, new ApiResponse<TripResponse>
                {
                    Success = false,
                    Message = "An error occurred"
                });
            }
        }

        /// <summary>
        /// Get detailed information about a specific trip
        /// </summary>
        [HttpGet("{tripId}")]
        [ProducesResponseType(typeof(ApiResponse<TripResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetTripDetails(long tripId)
        {
            try
            {
                var userId = _authUser.UserId;
                var driverId = await _assignmentService.GetDriverIdByUserAsync(userId);

                var response = await _tripService.GetTripByIdAsync(tripId);

                if (!response.Success)
                {
                    return NotFound(new ApiResponse<TripResponse>
                    {
                        Success = false,
                        Message = "Trip not found"
                    });
                }

                // Verify trip belongs to this driver
                if (response.Result.DriverId != driverId)
                {
                    _logger.LogWarning("Driver {DriverId} attempted to access trip {TripId} not assigned to them",
                        driverId, tripId);
                    return Forbid();
                }

                return Ok(new ApiResponse<TripResponse>
                {
                    Success = true,
                    Message = "Trip details retrieved",
                    Data = MapToTripResponse(response.Result)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving trip {TripId}", tripId);
                return StatusCode(500, new ApiResponse<TripResponse>
                {
                    Success = false,
                    Message = "An error occurred"
                });
            }
        }

        /// <summary>
        /// Start a trip
        /// </summary>
        [HttpPost("{tripId}/start")]
        [ProducesResponseType(typeof(ApiResponse<TripResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> StartTrip(long tripId, [FromBody] StartTripRequest request)
        {
            try
            {
                if (request.TripId != tripId)
                {
                    return BadRequest(new ApiResponse<TripResponse>
                    {
                        Success = false,
                        Message = "Trip ID mismatch"
                    });
                }

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    return BadRequest(new ApiResponse<TripResponse>
                    {
                        Success = false,
                        Message = "Validation failed",
                        Errors = errors
                    });
                }

                var dto = new StartTripDto
                {
                    TripId = request.TripId,
                    StartOdometer = request.StartOdometer,
                    Latitude = request.Latitude,
                    Longitude = request.Longitude,
                    LatitudeAccuracy = request.LatitudeAccuracy,
                    Notes = request.Notes
                };

                var response = await _tripService.StartTripAsync(dto);

                if (!response.Success)
                {
                    return BadRequest(new ApiResponse<TripResponse>
                    {
                        Success = false,
                        Message = response.Message
                    });
                }

                return Ok(new ApiResponse<TripResponse>
                {
                    Success = true,
                    Message = response.Message,
                    Data = MapToTripResponse(response.Result)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting trip {TripId}", tripId);
                return StatusCode(500, new ApiResponse<TripResponse>
                {
                    Success = false,
                    Message = "An error occurred while starting the trip"
                });
            }
        }

        /// <summary>
        /// Complete a trip
        /// </summary>
        [HttpPost("{tripId}/complete")]
        [ProducesResponseType(typeof(ApiResponse<TripResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CompleteTrip(long tripId, [FromBody] CompleteTripRequest request)
        {
            try
            {
                if (request.TripId != tripId)
                {
                    return BadRequest(new ApiResponse<TripResponse>
                    {
                        Success = false,
                        Message = "Trip ID mismatch"
                    });
                }

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    return BadRequest(new ApiResponse<TripResponse>
                    {
                        Success = false,
                        Message = "Validation failed",
                        Errors = errors
                    });
                }

                var dto = new CompleteTripDto
                {
                    TripId = request.TripId,
                    EndOdometer = request.EndOdometer,
                    ActualFuelCost = request.ActualFuelCost,
                    Latitude = request.Latitude,
                    Longitude = request.Longitude,
                    LatitudeAccuracy = request.LatitudeAccuracy,
                    Notes = request.Notes
                };

                var response = await _tripService.CompleteTripAsync(dto);

                if (!response.Success)
                {
                    return BadRequest(new ApiResponse<TripResponse>
                    {
                        Success = false,
                        Message = response.Message
                    });
                }

                return Ok(new ApiResponse<TripResponse>
                {
                    Success = true,
                    Message = response.Message,
                    Data = MapToTripResponse(response.Result)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing trip {TripId}", tripId);
                return StatusCode(500, new ApiResponse<TripResponse>
                {
                    Success = false,
                    Message = "An error occurred while completing the trip"
                });
            }
        }

        /// <summary>
        /// Update trip location (for real-time tracking during trip)
        /// Mobile app should send location every 10-30 seconds
        /// Smart filtering is applied in background to save only significant checkpoints
        /// </summary>
        [HttpPost("{tripId}/location")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateLocation(long tripId, [FromBody] UpdateLocationRequest request)
        {
            try
            {
                if (request.TripId != tripId)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Trip ID mismatch"
                    });
                }

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Invalid location data",
                        Errors = errors
                    });
                }

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                var locationUpdate = new LocationUpdate
                {
                    TripId = tripId,
                    Latitude = request.Latitude,
                    Longitude = request.Longitude,
                    Accuracy = request.Accuracy,
                    Speed = request.Speed,
                    Heading = request.Heading,
                    UserId = userId,
                    DeviceId = request.DeviceId
                };

                var response = await _locationService.UpdateTripLocationAsync(locationUpdate);

                // Return appropriate status code based on response
                if (!response.Success)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = response.Message
                    });
                }

                return Ok(new ApiResponse<object>
                {
                    Success = response.Success,
                    Message = response.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating location for trip {TripId}", tripId);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Error updating location"
                });
            }
        }

        // Add endpoint to get trip locations
        [HttpGet("{tripId}/locations")]
        [ProducesResponseType(typeof(ApiResponse<List<TripLocationDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTripLocations(long tripId)
        {
            try
            {
                var locations = await _locationService.GetTripLocationsAsync(tripId);

                return Ok(new ApiResponse<List<TripLocationDto>>
                {
                    Success = true,
                    Message = $"Retrieved {locations.Count} location(s)",
                    Data = locations
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting locations for trip {TripId}", tripId);
                return StatusCode(500, new ApiResponse<List<TripLocationDto>>
                {
                    Success = false,
                    Message = "Error retrieving locations"
                });
            }
        }

        #region Helper Methods

        private TripResponse MapToTripResponse(TripDto dto)
        {
            return new TripResponse
            {
                TripId = dto.Id,
                TripNumber = dto.TripNumber,
                VehicleId = dto.VehicleId,
                VehiclePlateNo = dto.VehiclePlateNo,
                VehicleMakeModel = $"{dto.VehicleMake} {dto.VehicleModel}".Trim(),
                VehicleMileage = dto.VehicleMileage,
                Origin = dto.Origin,
                Destination = dto.Destination,
                Purpose = dto.Purpose,
                Description = dto.Description,
                ScheduledStartDate = dto.ScheduledStartDate,
                ScheduledEndDate = dto.ScheduledEndDate,
                ActualStartDate = dto.ActualStartDate,
                ActualEndDate = dto.ActualEndDate,
                EstimatedDistance = dto.EstimatedDistance,
                ActualDistance = dto.ActualDistance,
                EstimatedFuelCost = dto.EstimatedFuelCost,
                ActualFuelCost = dto.ActualFuelCost,
                StartOdometer = dto.StartOdometer,
                EndOdometer = dto.EndOdometer,
                Status = dto.Status.ToString(),
                Priority = dto.Priority.ToString(),
                Notes = dto.Notes,
                RequiresApproval = dto.RequiresApproval,
                HasSuspiciousLocation = dto.HasSuspiciousLocation
            };
        }

        #endregion
    }

}
