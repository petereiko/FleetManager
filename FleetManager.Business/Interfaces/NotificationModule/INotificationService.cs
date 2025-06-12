using FleetManager.Business.DataObjects;
using FleetManager.Business.Enums;
using FleetManager.Business.UtilityModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Interfaces.NotificationModule
{
    public interface INotificationService
    {
        Task<MessageResponse<NotificationDto>> CreateAsync(string userId,string title,string message,NotificationType type,object? data = null);
        Task<List<NotificationDto>> GetRecentNotificationsAsync(string userId);
        Task<bool> MarkAsReadAsync(string userId, long notificationId);
        Task MarkAllReadAsync(string userId);
    }
}
