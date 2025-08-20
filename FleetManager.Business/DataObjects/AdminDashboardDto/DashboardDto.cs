using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.AdminDashboardDto
{
    public class DashboardDto
    {
        public DateTimeOffset GeneratedAt { get; set; }
        public DashboardCountsDto Counts { get; set; } = new();
        public DashboardMoneyDto Money { get; set; } = new();
        public List<MonthPointDto> FuelByMonthPreview { get; set; } = new(); // optional small preview
        public List<MonthPointDto> MaintenanceByMonthPreview { get; set; } = new();
        public List<RecentTicketDto> RecentMaintenanceTickets { get; set; } = new();
        public List<RecentFuelDto> RecentFuelLogs { get; set; } = new();
        public List<ContactDto> RecentContacts { get; set; } = new();
        public string CurrencySymbol { get; set; } = "NGN";
        public bool CacheHit { get; set; } = false;
        public string TimeZoneId { get; set; }
    }
}
