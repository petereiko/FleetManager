using FleetManager.Business;
using FleetManager.Business.DataObjects.ApiModels;
using FleetManager.Business.Enums;
using FleetManager.Business.Interfaces.DriverVehicleModule;
using FleetManager.Business.Interfaces.UserModule;
using FleetManager.Business.Interfaces.VehicleModule;
using FleetManager.Business.UtilityModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FleetManager.App.Controllers
{

    [Route("api/driver/vehicles")]
    [ApiController]
    [Authorize(Policy = "DriverApi")]
    public class DriverVehiclesApiController : ControllerBase
    {
        private readonly IDriverVehicleService _assignmentService;
        private readonly IAdminVehicleService _vehicleService;
        private readonly ILogger<DriverVehiclesApiController> _logger;
        private readonly IAuthUser _authUser;

        public DriverVehiclesApiController(
            IDriverVehicleService assignmentService,
            IAdminVehicleService vehicleService,
            ILogger<DriverVehiclesApiController> logger,
            IAuthUser authUser)
        {
            _assignmentService = assignmentService;
            _vehicleService = vehicleService;
            _logger = logger;
            _authUser = authUser;
        }

        /// <summary>
        /// Get all vehicles assigned to the current driver
        /// </summary>
        [HttpGet("assigned")]
        [ProducesResponseType(typeof(ApiResponse<List<AssignedVehicleResponse>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAssignedVehicles()
        {
            try
            {
                var userId = _authUser.UserId;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new ApiResponse<List<AssignedVehicleResponse>>
                    {
                        Success = false,
                        Message = "User not authenticated"
                    });
                }

                var driverId = await _assignmentService.GetDriverIdByUserAsync(userId);

                if (driverId == 0)
                {
                    return Ok(new ApiResponse<List<AssignedVehicleResponse>>
                    {
                        Success = false,
                        Message = "Driver profile not found",
                        Data = new List<AssignedVehicleResponse>()
                    });
                }

                var assignments = _assignmentService
                    .QueryAssignmentsByDriver(driverId)
                    .OrderByDescending(a => a.StartDate)
                    .ToList();

                var response = new List<AssignedVehicleResponse>();

                foreach (var assignment in assignments)
                {
                    // ✅ Get main image from service
                    var mainImagePath = await _vehicleService.GetVehicleMainImageUrlAsync(assignment.VehicleId);

                    response.Add(new AssignedVehicleResponse
                    {
                        VehicleId = assignment.VehicleId,
                        MakeModel = assignment.VehicleMakeModel ?? "Unknown Vehicle",
                        PlateNo = assignment.PlateNo ?? "",
                        StartDate = assignment.StartDate,
                        EndDate = assignment.EndDate,
                        IsActive = assignment.EndDate == null || assignment.EndDate > DateTime.UtcNow,
                        MainImageUrl = mainImagePath != null
                            ? UrlHelper.ToAbsoluteUrl(HttpContext, mainImagePath)
                            : null
                    });
                }

                return Ok(new ApiResponse<List<AssignedVehicleResponse>>
                {
                    Success = true,
                    Message = $"Found {response.Count} vehicle assignment(s)",
                    Data = response
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access attempt to assigned vehicles");
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving assigned vehicles");

                return StatusCode(500, new ApiResponse<List<AssignedVehicleResponse>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving assigned vehicles"
                });
            }
        }

        /// <summary>
        /// Get currently active vehicle assignment for the driver
        /// </summary>
        [HttpGet("current")]
        [ProducesResponseType(typeof(ApiResponse<AssignedVehicleResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCurrentAssignment()
        {
            try
            {
                var userId = _authUser.UserId;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new ApiResponse<AssignedVehicleResponse>
                    {
                        Success = false,
                        Message = "User not authenticated"
                    });
                }

                var driverId = await _assignmentService.GetDriverIdByUserAsync(userId);

                if (driverId == 0)
                {
                    return NotFound(new ApiResponse<AssignedVehicleResponse>
                    {
                        Success = false,
                        Message = "Driver profile not found"
                    });
                }

                var currentAssignment = _assignmentService
                    .QueryAssignmentsByDriver(driverId)
                    .Where(a => a.EndDate == null || a.EndDate > DateTime.UtcNow)
                    .OrderByDescending(a => a.StartDate)
                    .FirstOrDefault();

                if (currentAssignment == null)
                {
                    return Ok(new ApiResponse<AssignedVehicleResponse>
                    {
                        Success = false,
                        Message = "No active vehicle assignment found",
                        Data = null
                    });
                }

                var mainImagePath = await _vehicleService.GetVehicleMainImageUrlAsync(currentAssignment.VehicleId);

                var response = new AssignedVehicleResponse
                {
                    VehicleId = currentAssignment.VehicleId,
                    MakeModel = currentAssignment.VehicleMakeModel ?? "Unknown Vehicle",
                    PlateNo = currentAssignment.PlateNo ?? "",
                    StartDate = currentAssignment.StartDate,
                    EndDate = currentAssignment.EndDate,
                    IsActive = true,
                    MainImageUrl = mainImagePath != null
                        ? UrlHelper.ToAbsoluteUrl(HttpContext, mainImagePath)
                        : null
                };

                return Ok(new ApiResponse<AssignedVehicleResponse>
                {
                    Success = true,
                    Message = "Current assignment retrieved",
                    Data = response
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving current assignment");
                return StatusCode(500, new ApiResponse<AssignedVehicleResponse>
                {
                    Success = false,
                    Message = "An error occurred while retrieving current assignment"
                });
            }
        }

        /// <summary>
        /// Get detailed information about a specific vehicle
        /// </summary>
        [HttpGet("{vehicleId}")]
        [ProducesResponseType(typeof(ApiResponse<VehicleDetailsResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetVehicleDetails(long vehicleId)
        {
            try
            {
                var userId = _authUser.UserId;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new ApiResponse<VehicleDetailsResponse>
                    {
                        Success = false,
                        Message = "User not authenticated"
                    });
                }

                var driverId = await _assignmentService.GetDriverIdByUserAsync(userId);

                // ✅ Verify this vehicle is/was assigned to this driver
                var isAssigned = _assignmentService
                    .QueryAssignmentsByDriver(driverId)
                    .Any(a => a.VehicleId == vehicleId);

                if (!isAssigned)
                {
                    _logger.LogWarning("Driver {DriverId} attempted to access vehicle {VehicleId} not assigned to them",
                        driverId, vehicleId);
                    return Forbid();
                }

                var vehicleDto = await _vehicleService.GetVehicleByIdAsync(vehicleId);

                if (vehicleDto == null)
                {
                    return NotFound(new ApiResponse<VehicleDetailsResponse>
                    {
                        Success = false,
                        Message = "Vehicle not found"
                    });
                }

                // ✅ Get assignment dates for this driver
                var assignment = _assignmentService
                    .QueryAssignmentsByDriver(driverId)
                    .FirstOrDefault(a => a.VehicleId == vehicleId);

                // ✅ Get photos from service (not DbContext)
                var photos = await _vehicleService.GetVehiclePhotosAsync(vehicleId);

                var response = new VehicleDetailsResponse
                {
                    VehicleId = vehicleDto.Id ?? 0,
                    Make = vehicleDto.Make ?? "",
                    Model = vehicleDto.Model ?? "",
                    Year = vehicleDto.Year,
                    PlateNo = vehicleDto.PlateNo ?? "",
                    Color = vehicleDto.Color ?? "",
                    RegistrationDate = vehicleDto.RegistrationDate,
                    LastServiceDate = vehicleDto.LastServiceDate,
                    Mileage = vehicleDto.Mileage,
                    FuelType = vehicleDto.FuelType.ToString(),
                    TransmissionType = vehicleDto.TransmissionType.ToString(),
                    VehicleStatus = vehicleDto.VehicleStatus.ToString(),
                    VehicleType = vehicleDto.VehicleType.ToString(),
                    AssignmentStartDate = assignment?.StartDate,
                    AssignmentEndDate = assignment?.EndDate,
                    Photos = photos.Select(p => new VehiclePhotoDto
                    {
                        Id = p.Id,
                        FileName = p.FileName ?? "",
                        FileUrl = UrlHelper.ToAbsoluteUrl(HttpContext, p.FilePath)
                    }).ToList()
                };

                return Ok(new ApiResponse<VehicleDetailsResponse>
                {
                    Success = true,
                    Message = "Vehicle details retrieved",
                    Data = response
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access to vehicle {VehicleId}", vehicleId);
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving vehicle details for {VehicleId}", vehicleId);
                return StatusCode(500, new ApiResponse<VehicleDetailsResponse>
                {
                    Success = false,
                    Message = "An error occurred while retrieving vehicle details"
                });
            }
        }
    }
}
