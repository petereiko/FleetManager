using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.AdminDashboardDto
{
    public class RecentTicketDto
    {
        public long TicketId { get; set; }
        public long VehicleId { get; set; }
        public string VehiclePlateNo { get; set; } = "";
        public long DriverId { get; set; }
        public string DriverName { get; set; } = "";
        public string Status { get; set; } = "";
        public DateTime CreatedDate { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public decimal? InvoiceAmount { get; set; }
        public string Subject { get; set; } = "";
    }
}
