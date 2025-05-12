using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Database.Entities
{
    public class ActivityLog:BaseEntity
    {
        public string? Action { get; set; } 
        public string? Description { get; set; } 
    }
}
