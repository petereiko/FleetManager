using FleetManager.Business.DataObjects;
using FleetManager.Business.DataObjects.ApiModels;
using FleetManager.Business.Enums;
using FleetManager.Business.Interfaces.DriverVehicleModule;
using FleetManager.Business.Interfaces.FuelLogModule;
using FleetManager.Business.Interfaces.UserModule;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FleetManager.App.Controllers
{
    [Route("api/driver/fuel-logs")]
    [ApiController]
    [Authorize(Policy = "DriverApi")]
    public class DriverFuelLogApiController : ControllerBase
    {
        private readonly IFuelLogService _fuelLogService;
        private readonly IDriverVehicleService _assignmentService;
        private readonly IAuthUser _authUser;
        private readonly ILogger<DriverFuelLogApiController> _logger;
        private readonly IWebHostEnvironment _environment;

        public DriverFuelLogApiController(
            IFuelLogService fuelLogService,
            IDriverVehicleService assignmentService,
            IAuthUser authUser,
            ILogger<DriverFuelLogApiController> logger,
            IWebHostEnvironment environment)
        {
            _fuelLogService = fuelLogService;
            _assignmentService = assignmentService;
            _authUser = authUser;
            _logger = logger;
            _environment = environment;
        }

        /// <summary>
        /// Get all fuel logs for the current driver
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<List<FuelLogResponse>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetFuelLogs([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                var driverId = await _assignmentService.GetDriverIdByUserAsync(_authUser.UserId);

                if (driverId == 0)
                {
                    return Ok(new ApiResponse<List<FuelLogResponse>>
                    {
                        Success = false,
                        Message = "Driver profile not found",
                        Data = new List<FuelLogResponse>()
                    });
                }

                var logs = await _fuelLogService
                    .QueryByDriver(driverId)
                    .OrderByDescending(f => f.Date)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var response = logs.Select(l => new FuelLogResponse
                {
                    Id = l.Id,
                    VehicleId = l.VehicleId,
                    VehicleDescription = l.VehicleDescription,
                    LicenseNo = l.LicenseNo,
                    Date = l.Date,
                    Odometer = l.Odometer,
                    Volume = l.Volume,
                    Cost = l.Cost,
                    FuelType = l.FuelType.ToString(),
                    ReceiptUrl = !string.IsNullOrEmpty(l.ReceiptPath)
                        ? $"{Request.Scheme}://{Request.Host}{l.ReceiptPath}"
                        : null,
                    Notes = l.Notes
                }).ToList();

                return Ok(new ApiResponse<List<FuelLogResponse>>
                {
                    Success = true,
                    Message = $"Found {response.Count} fuel log(s)",
                    Data = response
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access to fuel logs");
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving fuel logs");
                return StatusCode(500, new ApiResponse<List<FuelLogResponse>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving fuel logs"
                });
            }
        }

        /// <summary>
        /// Get a specific fuel log by ID
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<FuelLogResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetFuelLog(long id)
        {
            try
            {
                var driverId = await _assignmentService.GetDriverIdByUserAsync(_authUser.UserId);
                var log = await _fuelLogService.GetByIdAsync(id);

                if (log == null)
                {
                    return NotFound(new ApiResponse<FuelLogResponse>
                    {
                        Success = false,
                        Message = "Fuel log not found"
                    });
                }

                // Verify ownership
                if (log.DriverId != driverId)
                {
                    _logger.LogWarning("Driver {DriverId} attempted to access fuel log {LogId} not belonging to them",
                        driverId, id);
                    return Forbid();
                }

                var response = new FuelLogResponse
                {
                    Id = log.Id,
                    VehicleId = log.VehicleId,
                    VehicleDescription = log.VehicleDescription,
                    LicenseNo = log.LicenseNo,
                    Date = log.Date,
                    Odometer = log.Odometer,
                    Volume = log.Volume,
                    Cost = log.Cost,
                    FuelType = log.FuelType.ToString(),
                    ReceiptUrl = !string.IsNullOrEmpty(log.ReceiptPath)
                        ? $"{Request.Scheme}://{Request.Host}{log.ReceiptPath}"
                        : null,
                    Notes = log.Notes
                };

                return Ok(new ApiResponse<FuelLogResponse>
                {
                    Success = true,
                    Message = "Fuel log retrieved",
                    Data = response
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving fuel log {Id}", id);
                return StatusCode(500, new ApiResponse<FuelLogResponse>
                {
                    Success = false,
                    Message = "An error occurred"
                });
            }
        }

        /// <summary>
        /// Create a new fuel log entry with optional receipt upload
        /// Use multipart/form-data for file upload
        /// </summary>
        [HttpPost]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(ApiResponse<FuelLogResponse>), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateFuelLog([FromForm] FuelLogRequest request, [FromForm] IFormFile? receiptFile)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    return BadRequest(new ApiResponse<FuelLogResponse>
                    {
                        Success = false,
                        Message = "Validation failed",
                        Errors = errors
                    });
                }

                var driverId = await _assignmentService.GetDriverIdByUserAsync(_authUser.UserId);

                if (driverId == 0)
                {
                    return BadRequest(new ApiResponse<FuelLogResponse>
                    {
                        Success = false,
                        Message = "Driver profile not found"
                    });
                }

                // Verify vehicle is assigned to driver
                var isVehicleAssigned = await _assignmentService
                    .QueryAssignmentsByDriver(driverId)
                    .AnyAsync(a => a.VehicleId == request.VehicleId);

                if (!isVehicleAssigned)
                {
                    return BadRequest(new ApiResponse<FuelLogResponse>
                    {
                        Success = false,
                        Message = "Vehicle is not assigned to you"
                    });
                }

                // Validate fuel type
                if (!Enum.TryParse<FuelType>(request.FuelType, out var fuelType))
                {
                    return BadRequest(new ApiResponse<FuelLogResponse>
                    {
                        Success = false,
                        Message = $"Invalid fuel type: {request.FuelType}"
                    });
                }

                // Validate receipt file if provided
                if (receiptFile != null)
                {
                    var validExtensions = new[] { ".jpg", ".jpeg", ".png", ".pdf" };
                    var extension = Path.GetExtension(receiptFile.FileName).ToLowerInvariant();

                    if (!validExtensions.Contains(extension))
                    {
                        return BadRequest(new ApiResponse<FuelLogResponse>
                        {
                            Success = false,
                            Message = "Invalid file type. Only JPG, PNG, and PDF files are allowed."
                        });
                    }

                    // Max file size: 5MB
                    if (receiptFile.Length > 5 * 1024 * 1024)
                    {
                        return BadRequest(new ApiResponse<FuelLogResponse>
                        {
                            Success = false,
                            Message = "File size must not exceed 5MB"
                        });
                    }
                }

                var input = new FuelLogInputDto
                {
                    DriverId = driverId,
                    VehicleId = request.VehicleId,
                    Date = request.Date,
                    Odometer = request.Odometer,
                    Volume = request.Volume,
                    Cost = request.Cost,
                    FuelType = fuelType,
                    Notes = request.Notes,
                    ReceiptFile = receiptFile
                };

                var result = await _fuelLogService.CreateAsync(input, _authUser.UserId);

                if (!result.Success)
                {
                    return BadRequest(new ApiResponse<FuelLogResponse>
                    {
                        Success = false,
                        Message = result.Message
                    });
                }

                var response = new FuelLogResponse
                {
                    Id = result.Result.Id,
                    VehicleId = result.Result.VehicleId,
                    VehicleDescription = result.Result.VehicleDescription,
                    LicenseNo = result.Result.LicenseNo,
                    Date = result.Result.Date,
                    Odometer = result.Result.Odometer,
                    Volume = result.Result.Volume,
                    Cost = result.Result.Cost,
                    FuelType = result.Result.FuelType.ToString(),
                    ReceiptUrl = !string.IsNullOrEmpty(result.Result.ReceiptPath)
                        ? $"{Request.Scheme}://{Request.Host}{result.Result.ReceiptPath}"
                        : null,
                    Notes = result.Result.Notes
                };

                return CreatedAtAction(
                    nameof(GetFuelLog),
                    new { id = response.Id },
                    new ApiResponse<FuelLogResponse>
                    {
                        Success = true,
                        Message = "Fuel log created successfully",
                        Data = response
                    }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating fuel log");
                return StatusCode(500, new ApiResponse<FuelLogResponse>
                {
                    Success = false,
                    Message = "An error occurred while creating fuel log"
                });
            }
        }

        /// <summary>
        /// Update an existing fuel log
        /// Use multipart/form-data for file upload
        /// </summary>
        [HttpPut("{id}")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(ApiResponse<FuelLogResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateFuelLog(long id, [FromForm] FuelLogRequest request, [FromForm] IFormFile? receiptFile)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    return BadRequest(new ApiResponse<FuelLogResponse>
                    {
                        Success = false,
                        Message = "Validation failed",
                        Errors = errors
                    });
                }

                var driverId = await _assignmentService.GetDriverIdByUserAsync(_authUser.UserId);

                // Verify ownership
                var existingLog = await _fuelLogService.GetByIdAsync(id);
                if (existingLog == null)
                {
                    return NotFound(new ApiResponse<FuelLogResponse>
                    {
                        Success = false,
                        Message = "Fuel log not found"
                    });
                }

                if (existingLog.DriverId != driverId)
                {
                    _logger.LogWarning("Driver {DriverId} attempted to update fuel log {LogId} not belonging to them",
                        driverId, id);
                    return Forbid();
                }

                // Validate fuel type
                if (!Enum.TryParse<FuelType>(request.FuelType, out var fuelType))
                {
                    return BadRequest(new ApiResponse<FuelLogResponse>
                    {
                        Success = false,
                        Message = $"Invalid fuel type: {request.FuelType}"
                    });
                }

                // Validate receipt file if provided
                if (receiptFile != null)
                {
                    var validExtensions = new[] { ".jpg", ".jpeg", ".png", ".pdf" };
                    var extension = Path.GetExtension(receiptFile.FileName).ToLowerInvariant();

                    if (!validExtensions.Contains(extension))
                    {
                        return BadRequest(new ApiResponse<FuelLogResponse>
                        {
                            Success = false,
                            Message = "Invalid file type. Only JPG, PNG, and PDF files are allowed."
                        });
                    }

                    if (receiptFile.Length > 5 * 1024 * 1024)
                    {
                        return BadRequest(new ApiResponse<FuelLogResponse>
                        {
                            Success = false,
                            Message = "File size must not exceed 5MB"
                        });
                    }
                }

                var input = new FuelLogInputDto
                {
                    DriverId = driverId,
                    VehicleId = request.VehicleId,
                    Date = request.Date,
                    Odometer = request.Odometer,
                    Volume = request.Volume,
                    Cost = request.Cost,
                    FuelType = fuelType,
                    Notes = request.Notes,
                    ReceiptFile = receiptFile
                };

                var result = await _fuelLogService.UpdateAsync(id, input, _authUser.UserId);

                if (!result.Success)
                {
                    return BadRequest(new ApiResponse<FuelLogResponse>
                    {
                        Success = false,
                        Message = result.Message
                    });
                }

                var response = new FuelLogResponse
                {
                    Id = result.Result.Id,
                    VehicleId = result.Result.VehicleId,
                    VehicleDescription = result.Result.VehicleDescription,
                    LicenseNo = result.Result.LicenseNo,
                    Date = result.Result.Date,
                    Odometer = result.Result.Odometer,
                    Volume = result.Result.Volume,
                    Cost = result.Result.Cost,
                    FuelType = result.Result.FuelType.ToString(),
                    ReceiptUrl = !string.IsNullOrEmpty(result.Result.ReceiptPath)
                        ? $"{Request.Scheme}://{Request.Host}{result.Result.ReceiptPath}"
                        : null,
                    Notes = result.Result.Notes
                };

                return Ok(new ApiResponse<FuelLogResponse>
                {
                    Success = true,
                    Message = "Fuel log updated successfully",
                    Data = response
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating fuel log {Id}", id);
                return StatusCode(500, new ApiResponse<FuelLogResponse>
                {
                    Success = false,
                    Message = "An error occurred while updating fuel log"
                });
            }
        }

        /// <summary>
        /// Delete a fuel log
        /// </summary>
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteFuelLog(long id)
        {
            try
            {
                var driverId = await _assignmentService.GetDriverIdByUserAsync(_authUser.UserId);

                // Verify ownership
                var existingLog = await _fuelLogService.GetByIdAsync(id);
                if (existingLog == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Fuel log not found"
                    });
                }

                if (existingLog.DriverId != driverId)
                {
                    _logger.LogWarning("Driver {DriverId} attempted to delete fuel log {LogId} not belonging to them",
                        driverId, id);
                    return Forbid();
                }

                var result = await _fuelLogService.DeleteAsync(id);

                if (!result.Success)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = result.Message
                    });
                }

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Fuel log deleted successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting fuel log {Id}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while deleting fuel log"
                });
            }
        }

        /// <summary>
        /// Get fuel type options for dropdown
        /// </summary>
        [HttpGet("fuel-types")]
        [ProducesResponseType(typeof(ApiResponse<List<FuelTypeOption>>), StatusCodes.Status200OK)]
        public IActionResult GetFuelTypes()
        {
            try
            {
                var fuelTypes = Enum.GetValues<FuelType>()
                    .Select(ft => new FuelTypeOption
                    {
                        Value = (int)ft,
                        Text = ft.ToString()
                    })
                    .ToList();

                return Ok(new ApiResponse<List<FuelTypeOption>>
                {
                    Success = true,
                    Message = "Fuel types retrieved",
                    Data = fuelTypes
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving fuel types");
                return StatusCode(500, new ApiResponse<List<FuelTypeOption>>
                {
                    Success = false,
                    Message = "An error occurred"
                });
            }
        }

        /// <summary>
        /// Get driver's assigned vehicles for fuel log dropdown
        /// </summary>
        [HttpGet("assigned-vehicles")]
        [ProducesResponseType(typeof(ApiResponse<List<VehicleOption>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAssignedVehicles()
        {
            try
            {
                var driverId = await _assignmentService.GetDriverIdByUserAsync(_authUser.UserId);

                var vehicles = await _assignmentService
                    .QueryAssignmentsByDriver(driverId)
                    .Where(a => a.EndDate == null || a.EndDate > DateTime.UtcNow) // Only active assignments
                    .Select(a => new VehicleOption
                    {
                        VehicleId = a.VehicleId,
                        Description = a.VehicleMakeModel,
                        PlateNo = a.PlateNo
                    })
                    .ToListAsync();

                return Ok(new ApiResponse<List<VehicleOption>>
                {
                    Success = true,
                    Message = $"Found {vehicles.Count} assigned vehicle(s)",
                    Data = vehicles
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving assigned vehicles");
                return StatusCode(500, new ApiResponse<List<VehicleOption>>
                {
                    Success = false,
                    Message = "An error occurred"
                });
            }
        }
    }

    
}

