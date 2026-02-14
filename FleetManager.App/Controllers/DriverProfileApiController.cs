using FleetManager.Business.DataObjects.ApiModels;
using FleetManager.Business.Interfaces.DriverProfileModule;
using FleetManager.Business.Interfaces.UserModule;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FleetManager.App.Controllers
{
    [Route("api/driver/profile")]
    [ApiController]
    [Authorize(Policy = "DriverApi")]
    public class DriverProfileApiController : ControllerBase
    {
        private readonly IDriverProfileService _profileService;
        private readonly IAuthUser _authUser;
        private readonly ILogger<DriverProfileApiController> _logger;

        public DriverProfileApiController(
            IDriverProfileService profileService,
            IAuthUser authUser,
            ILogger<DriverProfileApiController> logger)
        {
            _profileService = profileService;
            _authUser = authUser;
            _logger = logger;
        }

        /// <summary>
        /// Get the current driver's profile information
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<DriverProfileResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetProfile()
        {
            try
            {
                var userId = _authUser.UserId;

                if (string.IsNullOrEmpty(userId))
                {
                    return NotFound(new ApiResponse<DriverProfileResponse>
                    {
                        Success = false,
                        Message = "User ID not found"
                    });
                }

                var driverProfile = await _profileService.GetProfileAsync(userId);

                if (driverProfile == null)
                {
                    return NotFound(new ApiResponse<DriverProfileResponse>
                    {
                        Success = false,
                        Message = "Driver profile not found"
                    });
                }

                var response = new DriverProfileResponse
                {
                    Id = driverProfile.Id,
                    FullName = driverProfile.FullName ?? string.Empty,
                    FirstName = driverProfile.FirstName ?? string.Empty,
                    LastName = driverProfile.LastName ?? string.Empty,
                    Email = driverProfile.Email ?? string.Empty,
                    PhoneNumber = driverProfile.PhoneNumber ?? string.Empty,
                    Address = driverProfile.Address,
                    DateOfBirth = driverProfile.DateOfBirth,
                    Gender = driverProfile.Gender.ToString(),
                    EmploymentStatus = driverProfile.EmploymentStatus.ToString(),
                    LicenseNumber = driverProfile.LicenseNumber ?? string.Empty,
                    LicenseExpiryDate = driverProfile.LicenseExpiryDate,
                    LicenseCategory = driverProfile.LicenseCategory.ToString(),
                    ShiftStatus = driverProfile.ShiftStatus.ToString(),
                    IsActive = driverProfile.IsActive,
                    CreatedDate = driverProfile.CreatedDate,
                    PassportPhotoUrl = driverProfile.PassportFileName,
                    Documents = driverProfile.DriverDocuments.Select(d => new DriverDocumentResponse
                    {
                        Id = d.Id,
                        FileName = Path.GetFileName(d.FileName ?? string.Empty),
                        FileUrl = d.FileName ?? string.Empty,
                        DocumentType = d.DocumentType.ToString(),
                        UploadedDate = d.UploadedDate
                    }).ToList()
                };

                return Ok(new ApiResponse<DriverProfileResponse>
                {
                    Success = true,
                    Message = "Profile retrieved successfully",
                    Data = response
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving driver profile for user {UserId}", _authUser.UserId);
                return StatusCode(500, new ApiResponse<DriverProfileResponse>
                {
                    Success = false,
                    Message = "An error occurred while retrieving profile"
                });
            }
        }

        /// <summary>
        /// Change the current driver's password
        /// </summary>
        [HttpPost("change-password")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Validation failed",
                        Errors = errors
                    });
                }

                var userId = _authUser.UserId;

                if (string.IsNullOrEmpty(userId))
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "User ID not found"
                    });
                }

                var result = await _profileService.ChangePasswordAsync(
                    userId,
                    request.CurrentPassword,
                    request.NewPassword
                );

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
                    Message = result.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for user {UserId}", _authUser.UserId);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while changing password"
                });
            }
        }
    }
}
