using FleetManager.Business.DataObjects;
using FleetManager.Business.DataObjects.ApiModels;
using FleetManager.Business.Interfaces.NotificationModule;
using FleetManager.Business.Interfaces.UserModule;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace FleetManager.App.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = "SmartAuth")]
    //[Authorize] // Generic authorization - works for both web and mobile
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly IAuthUser _authUser;
        private readonly ILogger<NotificationsController> _logger;

        public NotificationsController(
            INotificationService notificationService,
            IAuthUser authUser,
            ILogger<NotificationsController> logger)
        {
            _notificationService = notificationService;
            _authUser = authUser;
            _logger = logger;
        }

        /// <summary>
        /// Get recent notifications for the current user
        /// Works for both web (Cookie) and mobile (JWT) authentication
        /// </summary>
        [HttpGet("get-recent")]
        [ProducesResponseType(typeof(ApiResponse<List<NotificationDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetRecent()
        {
            try
            {
                var userId = _authUser.UserId;
                var notifications = await _notificationService.GetRecentNotificationsAsync(userId);

                return Ok(new ApiResponse<List<NotificationDto>>
                {
                    Success = true,
                    Message = $"Found {notifications.Count} notification(s)",
                    Data = notifications
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving notifications for user {UserId}", _authUser.UserId);
                return StatusCode(500, new ApiResponse<List<NotificationDto>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving notifications"
                });
            }
        }

        /// <summary>
        /// Mark a specific notification as read
        /// </summary>
        [HttpPost("mark-read")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> MarkRead([FromBody] MarkReadRequest model)
        {
            try
            {
                if (model == null || model.NotificationId <= 0)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Invalid notification ID"
                    });
                }

                var userId = _authUser.UserId;
                var success = await _notificationService.MarkAsReadAsync(userId, model.NotificationId);

                if (!success)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Notification not found or already read"
                    });
                }

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Notification marked as read"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notification {NotificationId} as read", model.NotificationId);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred"
                });
            }
        }

        /// <summary>
        /// Mark all notifications as read for the current user
        /// </summary>
        [HttpPost("mark-all-read")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> MarkAllRead()
        {
            try
            {
                var userId = _authUser.UserId;
                await _notificationService.MarkAllReadAsync(userId);

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "All notifications marked as read"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking all notifications as read for user {UserId}", _authUser.UserId);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred"
                });
            }
        }

        /// <summary>
        /// Get unread notification count (useful for badge display)
        /// </summary>
        [HttpGet("unread-count")]
        [ProducesResponseType(typeof(ApiResponse<UnreadCountResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUnreadCount()
        {
            try
            {
                var userId = _authUser.UserId;
                var count = await _notificationService.GetUnreadCountAsync(userId);

                return Ok(new ApiResponse<UnreadCountResponse>
                {
                    Success = true,
                    Data = new UnreadCountResponse { Count = count }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unread count for user {UserId}", _authUser.UserId);
                return StatusCode(500, new ApiResponse<UnreadCountResponse>
                {
                    Success = false,
                    Message = "An error occurred"
                });
            }
        }

        /// <summary>
        /// Delete a notification
        /// </summary>
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteNotification(long id)
        {
            try
            {
                var userId = _authUser.UserId;
                var success = await _notificationService.DeleteNotificationAsync(userId, id);

                if (!success)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Notification not found"
                    });
                }

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Notification deleted"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting notification {Id}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred"
                });
            }
        }
    }

    // Add this model
    public class UnreadCountResponse
    {
        public int Count { get; set; }
    }




    //[ApiController]
    //[Route("api/[controller]")]
    //public class NotificationsController : ControllerBase
    //{
    //    private readonly INotificationService _notificationService;
    //    private readonly IAuthUser _authUser;
    //    private readonly IMemoryCache _cache;
    //    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    //    public NotificationsController(
    //        INotificationService notificationService,
    //        IAuthUser authUser,
    //        IMemoryCache cache)
    //    {
    //        _notificationService = notificationService;
    //        _authUser = authUser;
    //        _cache = cache;
    //    }

    //    [HttpGet]
    //    public async Task<IActionResult> GetRecent()
    //    {
    //        var userId = _authUser.UserId;
    //        var list = await _notificationService.GetRecentNotificationsAsync(userId);
    //        return Ok(list);
    //    }

    //    [HttpPost("mark-read")]
    //    public async Task<IActionResult> MarkRead([FromBody] MarkReadRequest model)
    //    {
    //        if (model == null || model.NotificationId <= 0)
    //            return BadRequest();

    //        var userId = _authUser.UserId;
    //        var success = await _notificationService.MarkAsReadAsync(userId, model.NotificationId);
    //        if (!success) return NotFound();

    //        return Ok();
    //    }

    //    [HttpPost("mark-all-read")]
    //    public async Task<IActionResult> MarkAllRead()
    //    {
    //        var userId = _authUser.UserId;
    //        await _notificationService.MarkAllReadAsync(userId);

    //        return Ok();
    //    }
    //}
}
