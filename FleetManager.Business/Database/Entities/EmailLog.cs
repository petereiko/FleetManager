using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Database.Entities
{
    public class EmailLog:BaseEntity
    {
        public string Recepient { get; set; }
        public string Subject { get; set; }
        public string? Message { get; set; }
        public bool IsDeleted { get; set; }
        public bool IsSent { get; set; }
        public int RetryCount { get; set; }

    }
}
