using FleetManager.Business.DataObjects.AdminDashboardDto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Interfaces.AdminDashboardModule
{
    public interface IAdminDashboardService
    {
        Task<DashboardDto> GetAdminDashboardAsync(DashboardRequestDto req, CancellationToken ct = default);

        // Async chart endpoints (small payloads, cached individually)
        Task<List<MonthPointDto>> GetFuelByMonthAsync(DashboardRequestDto req, CancellationToken ct = default);
        Task<List<MonthPointDto>> GetMaintenanceByMonthAsync(DashboardRequestDto req, CancellationToken ct = default);
        Task<List<KeyValueDto>> GetTicketsByStatusAsync(DashboardRequestDto req, CancellationToken ct = default);
        Task<List<TopVehicleDto>> GetTopVehiclesByFuelAsync(DashboardRequestDto req, int top = 10, CancellationToken ct = default);
        Task<List<PartCategorySpendDto>> GetMaintenanceCostByPartCategoryAsync(DashboardRequestDto req, int top = 10, CancellationToken ct = default);
        Task<List<RecentTicketDto>> GetRecentMaintenanceTicketsAsync(DashboardRequestDto req, CancellationToken ct = default);
        Task<List<RecentFuelDto>> GetRecentFuelLogsAsync(DashboardRequestDto req, CancellationToken ct = default);
        Task<List<ContactDto>> GetRecentContactsAsync(DashboardRequestDto req, CancellationToken ct = default);
    }
}
