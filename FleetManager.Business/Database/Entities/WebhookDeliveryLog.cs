using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Database.Entities
{
    public class WebhookDeliveryLog
    {
        public long Id { get; set; }
        public string EventName { get; set; }
        public long EntityId { get; set; } // tripId
        public string Url { get; set; }
        public string Payload { get; set; }
        public int AttemptCount { get; set; }
        public DateTime LastAttemptedAt { get; set; }
        public string LastError { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool Succeeded { get; set; }
    }

}
