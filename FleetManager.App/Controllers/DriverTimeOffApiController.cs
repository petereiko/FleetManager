using FleetManager.Business.DataObjects.ApiModels;
using FleetManager.Business.DataObjects.Schedule;
using FleetManager.Business.Enums;
using FleetManager.Business.Interfaces.DriverVehicleModule;
using FleetManager.Business.Interfaces.ScheduleModule;
using FleetManager.Business.Interfaces.UserModule;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FleetManager.App.Controllers
{
    [Route("api/driver/time-off")]
    [ApiController]
    [Authorize(Policy = "DriverApi")]
    public class DriverTimeOffApiController : ControllerBase
    {
        private readonly ITimeOffService _timeOffService;
        private readonly IDriverVehicleService _assignmentService;
        private readonly IAuthUser _authUser;
        private readonly ILogger<DriverTimeOffApiController> _logger;

        public DriverTimeOffApiController(
            ITimeOffService timeOffService,
            IDriverVehicleService assignmentService,
            IAuthUser authUser,
            ILogger<DriverTimeOffApiController> logger)
        {
            _timeOffService = timeOffService;
            _assignmentService = assignmentService;
            _authUser = authUser;
            _logger = logger;
        }

        /// <summary>
        /// Get all time-off requests for the current driver
        /// </summary>
        [HttpGet("requests")]
        [ProducesResponseType(typeof(ApiResponse<List<TimeOffRequestResponse>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetRequests([FromQuery] string? status = null)
        {
            try
            {
                var driverId = await _assignmentService.GetDriverIdByUserAsync(_authUser.UserId);

                if (driverId == 0)
                {
                    return Ok(new ApiResponse<List<TimeOffRequestResponse>>
                    {
                        Success = false,
                        Message = "Driver profile not found"
                    });
                }

                var requests = await _timeOffService.GetRequestsByDriverAsync(driverId);

                // Filter by status if provided
                if (!string.IsNullOrEmpty(status) && Enum.TryParse<TimeOffStatus>(status, true, out var statusEnum))
                {
                    requests = requests.Where(r => r.Status == statusEnum).ToList();
                }

                var response = requests.Select(r => MapToTimeOffResponse(r)).ToList();

                return Ok(new ApiResponse<List<TimeOffRequestResponse>>
                {
                    Success = true,
                    Message = $"Found {response.Count} time-off request(s)",
                    Data = response
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving time-off requests");
                return StatusCode(500, new ApiResponse<List<TimeOffRequestResponse>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving time-off requests"
                });
            }
        }

        /// <summary>
        /// Get a specific time-off request by ID
        /// </summary>
        [HttpGet("requests/{id}")]
        [ProducesResponseType(typeof(ApiResponse<TimeOffRequestResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetRequest(long id)
        {
            try
            {
                var driverId = await _assignmentService.GetDriverIdByUserAsync(_authUser.UserId);
                var request = await _timeOffService.GetRequestByIdAsync(id);

                if (request == null)
                {
                    return NotFound(new ApiResponse<TimeOffRequestResponse>
                    {
                        Success = false,
                        Message = "Time-off request not found"
                    });
                }

                // Verify ownership
                if (request.DriverId != driverId)
                {
                    _logger.LogWarning("Driver {DriverId} attempted to access time-off request {RequestId} not belonging to them",
                        driverId, id);
                    return Forbid();
                }

                return Ok(new ApiResponse<TimeOffRequestResponse>
                {
                    Success = true,
                    Message = "Request retrieved",
                    Data = MapToTimeOffResponse(request)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving time-off request {Id}", id);
                return StatusCode(500, new ApiResponse<TimeOffRequestResponse>
                {
                    Success = false,
                    Message = "An error occurred"
                });
            }
        }

        /// <summary>
        /// Create a new time-off request
        /// </summary>
        [HttpPost("requests")]
        [ProducesResponseType(typeof(ApiResponse<TimeOffRequestResponse>), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateRequest([FromBody] TimeOffRequestRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    return BadRequest(new ApiResponse<TimeOffRequestResponse>
                    {
                        Success = false,
                        Message = "Validation failed",
                        Errors = errors
                    });
                }

                // Additional validation
                if (request.EndDate < request.StartDate)
                {
                    return BadRequest(new ApiResponse<TimeOffRequestResponse>
                    {
                        Success = false,
                        Message = "End date must be on or after start date"
                    });
                }

                if (request.StartDate < DateTime.Today)
                {
                    return BadRequest(new ApiResponse<TimeOffRequestResponse>
                    {
                        Success = false,
                        Message = "Start date cannot be in the past"
                    });
                }

                var driverId = await _assignmentService.GetDriverIdByUserAsync(_authUser.UserId);

                if (driverId == 0)
                {
                    return BadRequest(new ApiResponse<TimeOffRequestResponse>
                    {
                        Success = false,
                        Message = "Driver profile not found"
                    });
                }

                if (!_authUser.CompanyBranchId.HasValue)
                {
                    return BadRequest(new ApiResponse<TimeOffRequestResponse>
                    {
                        Success = false,
                        Message = "Company branch not found"
                    });
                }

                var dto = new TimeOffRequestDto
                {
                    DriverId = driverId,
                    CategoryId = request.CategoryId,
                    StartDate = request.StartDate,
                    EndDate = request.EndDate,
                    Reason = request.Reason,
                    Status = TimeOffStatus.Pending,
                    CompanyBranchId = _authUser.CompanyBranchId.Value
                };

                // ✅ Call service method which returns MessageResponse<TimeOffRequestDto>
                var serviceResult = await _timeOffService.CreateRequestAsync(dto);

                // ✅ Check if service call was successful
                if (!serviceResult.Success)
                {
                    return BadRequest(new ApiResponse<TimeOffRequestResponse>
                    {
                        Success = false,
                        Message = serviceResult.Message ?? "Failed to create time-off request"
                    });
                }

                // ✅ Map the result to API response
                var response = MapToTimeOffResponse(serviceResult.Result);

                return CreatedAtAction(
                    nameof(GetRequest),
                    new { id = response.Id },
                    new ApiResponse<TimeOffRequestResponse>
                    {
                        Success = true,
                        Message = "Time-off request submitted successfully",
                        Data = response
                    }
                );
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized attempt to create time-off request");
                return StatusCode(403, new ApiResponse<TimeOffRequestResponse>
                {
                    Success = false,
                    Message = "You do not have permission to create time-off requests"
                });
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogError(ex, "Driver not found when creating time-off request");
                return BadRequest(new ApiResponse<TimeOffRequestResponse>
                {
                    Success = false,
                    Message = "Driver profile not found"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating time-off request");
                return StatusCode(500, new ApiResponse<TimeOffRequestResponse>
                {
                    Success = false,
                    Message = "An error occurred while creating the request"
                });
            }
        }

        /// <summary>
        /// Get time-off categories for dropdown
        /// </summary>
        [HttpGet("categories")]
        [ProducesResponseType(typeof(ApiResponse<List<TimeOffCategoryResponse>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCategories()
        {
            try
            {
                var categories = await _timeOffService.GetCategoriesAsync();

                var response = categories.Select(c => new TimeOffCategoryResponse
                {
                    Id = long.Parse(c.Value),
                    Name = c.Text,
                    Description = null // Add if available in your model
                }).ToList();

                return Ok(new ApiResponse<List<TimeOffCategoryResponse>>
                {
                    Success = true,
                    Message = $"Found {response.Count} categor{(response.Count == 1 ? "y" : "ies")}",
                    Data = response
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving time-off categories");
                return StatusCode(500, new ApiResponse<List<TimeOffCategoryResponse>>
                {
                    Success = false,
                    Message = "An error occurred"
                });
            }
        }

        /// <summary>
        /// Get time-off statistics for the current driver
        /// </summary>
        [HttpGet("stats")]
        [ProducesResponseType(typeof(ApiResponse<TimeOffStatsResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetStats()
        {
            try
            {
                var driverId = await _assignmentService.GetDriverIdByUserAsync(_authUser.UserId);

                if (driverId == 0)
                {
                    return Ok(new ApiResponse<TimeOffStatsResponse>
                    {
                        Success = false,
                        Message = "Driver profile not found"
                    });
                }

                var requests = await _timeOffService.GetRequestsByDriverAsync(driverId);

                var now = DateTime.UtcNow;
                var startOfMonth = new DateTime(now.Year, now.Month, 1);
                var startOfYear = new DateTime(now.Year, 1, 1);

                var stats = new TimeOffStatsResponse
                {
                    TotalRequests = requests.Count(),
                    PendingRequests = requests.Count(r => r.Status == TimeOffStatus.Pending),
                    ApprovedRequests = requests.Count(r => r.Status == TimeOffStatus.Approved),
                    DeniedRequests = requests.Count(r => r.Status == TimeOffStatus.Denied),
                    TotalDaysOff = requests
                        .Where(r => r.Status == TimeOffStatus.Approved)
                        .Sum(r => (r.EndDate - r.StartDate).Days + 1),
                    DaysOffThisMonth = requests
                        .Where(r => r.Status == TimeOffStatus.Approved && r.StartDate >= startOfMonth)
                        .Sum(r => (r.EndDate - r.StartDate).Days + 1),
                    DaysOffThisYear = requests
                        .Where(r => r.Status == TimeOffStatus.Approved && r.StartDate >= startOfYear)
                        .Sum(r => (r.EndDate - r.StartDate).Days + 1)
                };

                return Ok(new ApiResponse<TimeOffStatsResponse>
                {
                    Success = true,
                    Message = "Statistics retrieved",
                    Data = stats
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving time-off stats");
                return StatusCode(500, new ApiResponse<TimeOffStatsResponse>
                {
                    Success = false,
                    Message = "An error occurred"
                });
            }
        }

        /// <summary>
        /// Get upcoming approved time-off (next 30 days)
        /// </summary>
        [HttpGet("upcoming")]
        [ProducesResponseType(typeof(ApiResponse<List<TimeOffRequestResponse>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUpcoming()
        {
            try
            {
                var driverId = await _assignmentService.GetDriverIdByUserAsync(_authUser.UserId);

                if (driverId == 0)
                {
                    return Ok(new ApiResponse<List<TimeOffRequestResponse>>
                    {
                        Success = false,
                        Message = "Driver profile not found"
                    });
                }

                var requests = await _timeOffService.GetRequestsByDriverAsync(driverId);

                var now = DateTime.Today;
                var next30Days = now.AddDays(30);

                var upcoming = requests
                    .Where(r => r.Status == TimeOffStatus.Approved &&
                                r.StartDate >= now &&
                                r.StartDate <= next30Days)
                    .OrderBy(r => r.StartDate)
                    .Select(r => MapToTimeOffResponse(r))
                    .ToList();

                return Ok(new ApiResponse<List<TimeOffRequestResponse>>
                {
                    Success = true,
                    Message = $"Found {upcoming.Count} upcoming time-off request(s)",
                    Data = upcoming
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving upcoming time-off");
                return StatusCode(500, new ApiResponse<List<TimeOffRequestResponse>>
                {
                    Success = false,
                    Message = "An error occurred"
                });
            }
        }

        #region Helper Methods

        private TimeOffRequestResponse MapToTimeOffResponse(TimeOffRequestDto dto)
        {
            var daysRequested = (dto.EndDate - dto.StartDate).Days + 1;

            return new TimeOffRequestResponse
            {
                Id = dto.Id,
                CategoryId = dto.CategoryId,
                CategoryName = dto.CategoryName,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                DaysRequested = daysRequested,
                Reason = dto.Reason,
                Status = dto.Status.ToString(),
                RequestedAt = dto.RequestedAt,
                ReviewedAt = dto.ReviewedAt,
                ReviewedByName = dto.ReviewedByName,
                AdminNotes = dto.AdminNotes
            };
        }

        #endregion
    }

}
