using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.TripsDto
{
    public class SimpleDriverDto
    {
        public long Id { get; set; }                
        public string? IdentityUserId { get; set; }  
        public string FullName { get; set; } = string.Empty;
    }
}
