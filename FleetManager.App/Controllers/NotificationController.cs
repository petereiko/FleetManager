using FleetManager.Business.DataObjects;
using FleetManager.Business.Interfaces.NotificationModule;
using FleetManager.Business.Interfaces.UserModule;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FleetManager.App.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly IAuthUser _authUser;

        public NotificationsController(
            INotificationService notificationService,
            IAuthUser authUser)
        {
            _notificationService = notificationService;
            _authUser = authUser;
        }

        
        [HttpGet]
        public async Task<IActionResult> GetRecent()
        {
            var userId = _authUser.UserId;
            var list = await _notificationService.GetRecentNotificationsAsync(userId);
            return Ok(list);
        }

        
        [HttpPost("mark-read")]
        public async Task<IActionResult> MarkRead([FromBody] MarkReadRequest model)
        {
            if (model == null || model.NotificationId <= 0)
                return BadRequest();

            var userId = _authUser.UserId;
            var success = await _notificationService.MarkAsReadAsync(userId, model.NotificationId);
            if (!success) return NotFound();
            return Ok();
        }

        
        [HttpPost("mark-all-read")]
        public async Task<IActionResult> MarkAllRead()
        {
            var userId = _authUser.UserId;
            await _notificationService.MarkAllReadAsync(userId);
            return Ok();
        }

        
    }
}
