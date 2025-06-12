using FleetManager.Business.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects
{
    public class NotificationDto
    {
        public long Id { get; set; }
        public string Title { get; set; } = "";
        public string Message { get; set; } = "";
        public DateTime Timestamp { get; set; }
        public bool IsRead { get; set; }
        public NotificationType Type { get; set; }
        public string? Data { get; set; }
    }

    public class MarkReadRequest
    {
        public long NotificationId { get; set; }
    }
}
