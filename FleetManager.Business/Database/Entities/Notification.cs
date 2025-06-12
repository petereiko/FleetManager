using FleetManager.Business.Database.IdentityModels;
using FleetManager.Business.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Database.Entities
{
    public class Notification
    {
        public long Id { get; set; }

        public string UserId { get; set; }        // FK to AspNetUsers.Id
        public virtual ApplicationUser User { get; set; }

        [StringLength(100)]
        public string Title { get; set; }

        [StringLength(500)]
        public string Message { get; set; }

        public DateTime Timestamp { get; set; }   // when it was created

        public bool IsRead { get; set; } = false;

        [Required]
        public NotificationType Type { get; set; } = NotificationType.Info;

        /// <summary>
        /// Optional JSON payload for link / metadata
        /// </summary>
        public string? Data { get; set; }
    }
}
