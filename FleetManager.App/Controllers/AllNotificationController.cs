using FleetManager.Business.Interfaces.UserModule;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FleetManager.App.Controllers
{
    [Authorize]
    public class AllNotificationController : Controller
    {
        private readonly IAuthUser _authUser;
        private readonly ILogger<AllNotificationController> _logger;

        public AllNotificationController(
            IAuthUser authUser,
            ILogger<AllNotificationController> logger)
        {
            _authUser = authUser;
            _logger = logger;
        }

        /// <summary>
        /// Display all notifications page
        /// </summary>
        public IActionResult Index()
        {
            ViewBag.UserId = _authUser.UserId;
            ViewBag.UserName = _authUser.FullName;
            return View();
        }
    }
}
