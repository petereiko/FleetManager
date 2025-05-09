using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Database.Entities
{
    public class ActivityLog
    {
        public long Id { get; set; }

        // Who performed the activity
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string Role { get; set; } // e.g. Admin, Driver

        // What happened
        public string Action { get; set; } // e.g. "Created Driver", "Updated Vehicle", "Logged In"
        public string Description { get; set; } // Detailed description or extra info

        // When it happened
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        
    }
}
