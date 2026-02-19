using FleetManager.Business.DataObjects;
using FleetManager.Business.DataObjects.ApiModels;
using FleetManager.Business.Enums;
using FleetManager.Business.Interfaces.DriverVehicleModule;
using FleetManager.Business.Interfaces.FineAndTollModule;
using FleetManager.Business.Interfaces.UserModule;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FleetManager.App.Controllers
{
    [Route("api/driver/fine-tolls")]
    [ApiController]
    [Authorize(Policy = "DriverApi")]
    public class DriverFineTollApiController : ControllerBase
    {
        private readonly IFineAndTollService _fineService;
        private readonly IDriverVehicleService _assignmentService;
        private readonly IAuthUser _authUser;
        private readonly ILogger<DriverFineTollApiController> _logger;

        public DriverFineTollApiController(
            IFineAndTollService fineService,
            IDriverVehicleService assignmentService,
            IAuthUser authUser,
            ILogger<DriverFineTollApiController> logger)
        {
            _fineService = fineService;
            _assignmentService = assignmentService;
            _authUser = authUser;
            _logger = logger;
        }

        /// <summary>
        /// Get all fine/toll records for the current driver
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<List<FineTollResponse>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetFineTolls(
            [FromQuery] string? type = null,
            [FromQuery] string? status = null)
        {
            try
            {
                var list = await _fineService
                    .QueryByDriver(_authUser.UserId)
                    .OrderByDescending(f => f.CreatedDate)
                    .ToListAsync();

                // Filter by type if provided
                if (!string.IsNullOrEmpty(type) &&
                    Enum.TryParse<FineTollType>(type, true, out var typeEnum))
                {
                    list = list.Where(f => f.Type == typeEnum).ToList();
                }

                // Filter by status if provided
                if (!string.IsNullOrEmpty(status) &&
                    Enum.TryParse<FineTollStatus>(status, true, out var statusEnum))
                {
                    list = list.Where(f => f.Status == statusEnum).ToList();
                }

                var response = list.Select(f => MapToFineTollResponse(f)).ToList();

                return Ok(new ApiResponse<List<FineTollResponse>>
                {
                    Success = true,
                    Message = $"Found {response.Count} record(s)",
                    Data = response
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access to fine/toll records");
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving fine/toll records");
                return StatusCode(500, new ApiResponse<List<FineTollResponse>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving records"
                });
            }
        }

        /// <summary>
        /// Get a specific fine/toll record by ID
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<FineTollResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetFineToll(long id)
        {
            try
            {
                var record = await _fineService.GetByIdAsync(id);

                if (record == null)
                {
                    return NotFound(new ApiResponse<FineTollResponse>
                    {
                        Success = false,
                        Message = "Fine/Toll record not found"
                    });
                }

                return Ok(new ApiResponse<FineTollResponse>
                {
                    Success = true,
                    Message = "Record retrieved",
                    Data = MapToFineTollResponse(record)
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex,
                    "Driver {UserId} attempted to access fine/toll {Id} not belonging to them",
                    _authUser.UserId, id);
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving fine/toll record {Id}", id);
                return StatusCode(500, new ApiResponse<FineTollResponse>
                {
                    Success = false,
                    Message = "An error occurred"
                });
            }
        }

        /// <summary>
        /// Log a new fine or toll
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<FineTollResponse>), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateFineToll([FromBody] FineTollRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    return BadRequest(new ApiResponse<FineTollResponse>
                    {
                        Success = false,
                        Message = "Validation failed",
                        Errors = errors
                    });
                }

                // Validate type
                if (!Enum.TryParse<FineTollType>(request.Type, true, out var fineTollType))
                {
                    return BadRequest(new ApiResponse<FineTollResponse>
                    {
                        Success = false,
                        Message = $"Invalid type '{request.Type}'. Valid values are: Fine, Toll"
                    });
                }

                // Verify vehicle is assigned to driver
                var driverId = await _assignmentService
                    .GetDriverIdByUserAsync(_authUser.UserId);

                var isVehicleAssigned = await _assignmentService
                    .QueryAssignmentsByDriver(driverId)
                    .AnyAsync(a => a.VehicleId == request.VehicleId);

                if (!isVehicleAssigned)
                {
                    return BadRequest(new ApiResponse<FineTollResponse>
                    {
                        Success = false,
                        Message = "Vehicle is not assigned to you"
                    });
                }

                var input = new FineAndTollInputDto
                {
                    VehicleId = request.VehicleId,
                    Type = fineTollType,
                    Title = request.Title,
                    Amount = request.Amount,
                    Currency = request.Currency,
                    Reason = request.Reason,
                    Notes = request.Notes ?? string.Empty,
                    IsMinimal = request.IsMinimal
                };

                var result = await _fineService.CreateAsync(input, _authUser.UserId);

                if (!result.Success)
                {
                    return BadRequest(new ApiResponse<FineTollResponse>
                    {
                        Success = false,
                        Message = result.Message
                    });
                }

                var response = MapToFineTollResponse(result.Result);

                return CreatedAtAction(
                    nameof(GetFineToll),
                    new { id = response.Id },
                    new ApiResponse<FineTollResponse>
                    {
                        Success = true,
                        Message = $"{fineTollType} logged successfully",
                        Data = response
                    }
                );
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized create attempt by {UserId}", _authUser.UserId);
                return StatusCode(403, new ApiResponse<FineTollResponse>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating fine/toll record");
                return StatusCode(500, new ApiResponse<FineTollResponse>
                {
                    Success = false,
                    Message = "An error occurred while creating the record"
                });
            }
        }

        /// <summary>
        /// Delete an unpaid fine/toll record
        /// Only unpaid records can be deleted
        /// </summary>
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteFineToll(long id)
        {
            try
            {
                var result = await _fineService.DeleteAsync(id, _authUser.UserId);

                if (!result.Success)
                {
                    // Record not found
                    if (result.Message.Contains("not found"))
                    {
                        return NotFound(new ApiResponse<object>
                        {
                            Success = false,
                            Message = result.Message
                        });
                    }

                    // Paid record cannot be deleted
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = result.Message
                    });
                }

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = result.Message
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex,
                    "Driver {UserId} attempted to delete fine/toll {Id} not belonging to them",
                    _authUser.UserId, id);
                return StatusCode(403, new ApiResponse<object>
                {
                    Success = false,
                    Message = "You can only delete your own fine/toll records"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting fine/toll record {Id}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while deleting the record"
                });
            }
        }

        /// <summary>
        /// Get fine/toll type options for dropdown
        /// </summary>
        [HttpGet("types")]
        [ProducesResponseType(typeof(ApiResponse<List<FineTollTypeOption>>), StatusCodes.Status200OK)]
        public IActionResult GetFineTollTypes()
        {
            var types = Enum.GetValues<FineTollType>()
                .Select(t => new FineTollTypeOption
                {
                    Value = (int)t,
                    Text = t.ToString()
                })
                .ToList();

            return Ok(new ApiResponse<List<FineTollTypeOption>>
            {
                Success = true,
                Message = "Types retrieved",
                Data = types
            });
        }


        /// <summary>
        /// Get fine/toll status options for dropdown
        /// </summary>
        [HttpGet("status")]
        [ProducesResponseType(typeof(ApiResponse<List<FineTollStatusOption>>), StatusCodes.Status200OK)]
        public IActionResult GetFineTollStatus()
        {
            var types = Enum.GetValues<FineTollStatus>()
                .Select(t => new FineTollStatusOption
                {
                    Value = (int)t,
                    Text = t.ToString()
                })
                .ToList();

            return Ok(new ApiResponse<List<FineTollStatusOption>>
            {
                Success = true,
                Message = "Status retrieved",
                Data = types
            });
        }

        #region Helper Methods

        private FineTollResponse MapToFineTollResponse(FineAndTollDto dto)
        {
            return new FineTollResponse
            {
                Id = dto.Id,
                VehicleId = dto.VehicleId,
                VehicleDescription = dto.VehicleDescription,
                Type = dto.Type.ToString(),
                Title = dto.Title,
                Amount = dto.Amount,
                Currency = dto.Currency,
                Reason = dto.Reason,
                Notes = dto.Notes,
                IsMinimal = dto.IsMinimal,
                Status = dto.Status.ToString(),
                PaidDate = dto.PaidDate,
                DateLogged = dto.CreatedDate
            };
        }

        #endregion
    }
}
