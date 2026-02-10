using FleetManager.Business;
using FleetManager.Business.Database.IdentityModels;
using FleetManager.Business.DataObjects.ApiModels;
using FleetManager.Business.DataObjects.DashboardDriverDto;
using FleetManager.Business.Interfaces.DriverDashboardModule;
using FleetManager.Business.Interfaces.UserModule;
using FleetManager.Business.UtilityModels.AuthenticationModule;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FleetManager.App.Controllers
{
    // Controllers/API/DriverApiController.cs
    [Authorize(Policy = "DriverApi")]
    [Route("api/driver")]
    [ApiController]
    public class DriverApiController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly IDriverDashboardService _dashboardService;
        private readonly FleetManagerDbContext _context;
        private readonly ILogger<DriverApiController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IAuthUser _authUser;

        public DriverApiController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IJwtTokenService jwtTokenService,
            IDriverDashboardService dashboardService,
            FleetManagerDbContext context,
            ILogger<DriverApiController> logger,
            IConfiguration configuration,
            IAuthUser authUser)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _jwtTokenService = jwtTokenService;
            _dashboardService = dashboardService;
            _context = context;
            _logger = logger;
            _configuration = configuration;
            _authUser = authUser;
        }

        /// <summary>
        /// Driver Login - Authenticates driver and returns JWT token
        /// </summary>
        /// <param name="request">Login credentials</param>
        /// <returns>JWT token and driver profile</returns>
        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new LoginResponse
                    {
                        Success = false,
                        Message = "Invalid request data",
                    });
                }

                // Find user by email
                var user = await _userManager.FindByEmailAsync(request.Email);
                if (user == null)
                {
                    return Unauthorized(new LoginResponse
                    {
                        Success = false,
                        Message = "Invalid email or password"
                    });
                }

                // Check if email is confirmed
                if (!user.EmailConfirmed)
                {
                    return BadRequest(new LoginResponse
                    {
                        Success = false,
                        Message = "Your email has not been activated. Please activate your email first."
                    });
                }

                // Check if user is active
                if (!user.IsActive)
                {
                    return BadRequest(new LoginResponse
                    {
                        Success = false,
                        Message = "Your account has been deactivated. Please contact the administrator."
                    });
                }

                // Verify password
                var passwordCheck = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: false);
                if (!passwordCheck.Succeeded)
                {
                    return Unauthorized(new LoginResponse
                    {
                        Success = false,
                        Message = "Invalid email or password"
                    });
                }

                // Check if user has Driver role
                var roles = await _userManager.GetRolesAsync(user);
                if (!roles.Contains("Driver"))
                {
                    return Unauthorized(new LoginResponse
                    {
                        Success = false,
                        Message = "This login is only available for drivers. Please use the web application."
                    });
                }

                // Check if it's first login (require password reset)
                if (user.IsFirstLogin)
                {
                    return BadRequest(new LoginResponse
                    {
                        Success = false,
                        Message = "First time login detected. Please reset your password using the web application first."
                    });
                }

                // Get driver details
                var driver = await _context.Drivers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(d => d.UserId == user.Id);

                // Generate JWT token
                var token = await _jwtTokenService.GenerateTokenAsync(user);
                var expiryMinutes = int.Parse(_configuration["JwtSettings:ExpiryInMinutes"] ?? "1440");

                return Ok(new LoginResponse
                {
                    Success = true,
                    Message = "Login successful",
                    Token = token,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes),
                    Driver = new DriverProfileDto
                    {
                        Id = user.Id,
                        Email = user.Email ?? string.Empty,
                        FullName = $"{user.FirstName} {user.LastName}".Trim(),
                        PhoneNumber = user.PhoneNumber ?? string.Empty,
                        DriverId = driver?.Id,
                        LicenseNumber = driver?.LicenseNumber
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during driver login for email: {Email}", request.Email);
                return StatusCode(500, new LoginResponse
                {
                    Success = false,
                    Message = "An error occurred during login. Please try again later."
                });
            }
        }

        /// <summary>
        /// Get Driver Dashboard - Returns comprehensive dashboard data
        /// </summary>
        /// <returns>Dashboard data including stats, schedule, activities, etc.</returns>
        [HttpGet("dashboard")]
        [Authorize(Roles = "Driver")]
        [ProducesResponseType(typeof(ApiResponse<DriverDashboardDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetDashboard()
        {
            try
            {
                var response = await _dashboardService.GetDriverDashboardAsync();

                if (!response.Success)
                {
                    return Ok(new ApiResponse<DriverDashboardDto>
                    {
                        Success = false,
                        Message = response.Message,
                        Data = null
                    });
                }

                return Ok(new ApiResponse<DriverDashboardDto>
                {
                    Success = true,
                    Message = "Dashboard loaded successfully",
                    Data = response.Result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching driver dashboard for user: {UserId}", User.FindFirstValue(ClaimTypes.NameIdentifier));
                return StatusCode(500, new ApiResponse<DriverDashboardDto>
                {
                    Success = false,
                    Message = "An error occurred while loading dashboard",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Get Driver Profile - Returns current driver's profile information
        /// </summary>
        /// <returns>Driver profile details</returns>
        [HttpGet("profile")]
        [Authorize(Roles = "Driver")]
        [ProducesResponseType(typeof(ApiResponse<DriverProfileDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetProfile()
        {
            try
            {
                var userId = _authUser.UserId;
                var user = await _userManager.FindByIdAsync(userId);

                if (user == null)
                {
                    return NotFound(new ApiResponse<DriverProfileDto>
                    {
                        Success = false,
                        Message = "User not found"
                    });
                }

                var driver = await _context.Drivers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(d => d.UserId == user.Id);

                return Ok(new ApiResponse<DriverProfileDto>
                {
                    Success = true,
                    Message = "Profile retrieved successfully",
                    Data = new DriverProfileDto
                    {
                        Id = user.Id,
                        Email = user.Email ?? string.Empty,
                        FullName = $"{user.FirstName} {user.LastName}".Trim(),
                        PhoneNumber = user.PhoneNumber ?? string.Empty,
                        DriverId = driver?.Id,
                        LicenseNumber = driver?.LicenseNumber
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching driver profile");
                return StatusCode(500, new ApiResponse<DriverProfileDto>
                {
                    Success = false,
                    Message = "An error occurred while fetching profile"
                });
            }
        }

        /// <summary>
        /// Refresh Token - Generate a new JWT token
        /// </summary>
        /// <returns>New JWT token</returns>
        [HttpPost("refresh-token")]
        [Authorize(Roles = "Driver")]
        [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> RefreshToken()
        {
            try
            {
                var userId = _authUser.UserId;
                var user = await _userManager.FindByIdAsync(userId);

                if (user == null || !user.IsActive)
                {
                    return Unauthorized(new LoginResponse
                    {
                        Success = false,
                        Message = "Invalid user or inactive account"
                    });
                }

                var token = await _jwtTokenService.GenerateTokenAsync(user);
                var expiryMinutes = int.Parse(_configuration["JwtSettings:ExpiryInMinutes"] ?? "1440");

                return Ok(new LoginResponse
                {
                    Success = true,
                    Message = "Token refreshed successfully",
                    Token = token,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing token");
                return StatusCode(500, new LoginResponse
                {
                    Success = false,
                    Message = "An error occurred while refreshing token"
                });
            }
        }
    }
}
