using FleetManager.Business.Interfaces.DriverProfileModule;
using FleetManager.Business.Interfaces.UserModule;
using Microsoft.AspNetCore.Mvc;

namespace FleetManager.App.Controllers
{
    public class ProfileController : Controller
    {
        private readonly IAuthUser _authUser; 
        private readonly IDriverProfileService _profileService;
        public ProfileController(IAuthUser authUser, IDriverProfileService profileService)
        {
            _authUser = authUser;
            _profileService = profileService;
        }


        public async Task<IActionResult> Index()
        {
            var userId = _authUser.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                return NotFound("User ID is required.");
            }

            var driverProfile = await _profileService.GetProfileAsync(userId);

            if (driverProfile == null)
            {
                return NotFound("Driver profile not found.");
            }

            return View(driverProfile);
        }
    }
}
