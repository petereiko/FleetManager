using FleetManager.Business.Database.Entities;
using FleetManager.Business.DataObjects;
using FleetManager.Business.Enums;
using FleetManager.Business.Hubs;
using FleetManager.Business.Interfaces.NotificationModule;
using FleetManager.Business.Interfaces.UserModule;
using FleetManager.Business.UtilityModels;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace FleetManager.Business.Implementations.NotificationModule
{
    public class NotificationService : INotificationService
    {
        private readonly FleetManagerDbContext _db;
        private readonly IAuthUser _auth;
        private readonly IHubContext<NotificationHub> _hub;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            FleetManagerDbContext db,
            IAuthUser auth,
            IHubContext<NotificationHub> hub,
            ILogger<NotificationService> logger)
        {
            _db = db;
            _auth = auth;
            _hub = hub;
            _logger = logger;
        }

        public async Task<MessageResponse<NotificationDto>> CreateAsync(
            string userId,
            string title,
            string message,
            NotificationType type,
            object? data = null)
        {
            var resp = new MessageResponse<NotificationDto>();
            try
            {
                var entity = new Notification
                {
                    UserId = userId,
                    Title = title,
                    Message = message,
                    Timestamp = DateTime.UtcNow,
                    Type = type,
                    Data = data is null ? null :
                        JsonSerializer.Serialize(data, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })
                };

                _db.Notifications.Add(entity);
                await _db.SaveChangesAsync();

                var dto = new NotificationDto
                {
                    Id = entity.Id,
                    Title = entity.Title,
                    Message = entity.Message,
                    Timestamp = entity.Timestamp,
                    IsRead = entity.IsRead,
                    Type = entity.Type,
                    Data = entity.Data
                };

                // send via SignalR
                await _hub.Clients.User(userId)
                          .SendAsync("ReceiveNotification", dto);

                resp.Success = true;
                resp.Result = dto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating notification");
                resp.Message = "Could not create notification.";
            }

            return resp;
        }

        public async Task<List<NotificationDto>> GetRecentNotificationsAsync(string userId)
        {
            return await _db.Notifications
                .AsNoTracking()
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.Timestamp)
                .Take(50)
                .Select(n => new NotificationDto
                {
                    Id = n.Id,
                    Title = n.Title,
                    Message = n.Message,
                    Timestamp = n.Timestamp,
                    IsRead = n.IsRead,
                    Type = n.Type,
                    Data = n.Data
                })
                .ToListAsync();
        }

        public async Task<bool> MarkAsReadAsync(string userId, long notificationId)
        {
            var notif = await _db.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);
            if (notif == null) return false;

            notif.IsRead = true;
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task MarkAllReadAsync(string userId)
        {
            var unread = await _db.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            if (unread.Any())
            {
                unread.ForEach(n => n.IsRead = true);
                await _db.SaveChangesAsync();
            }
        }
    }
}
