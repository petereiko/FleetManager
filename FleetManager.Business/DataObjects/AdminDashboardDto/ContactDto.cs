using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.AdminDashboardDto
{
    public class ContactDto
    {
        public long Id { get; set; }
        public string ContactName { get; set; } = "";
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string? Services { get; set; }
    }
}
