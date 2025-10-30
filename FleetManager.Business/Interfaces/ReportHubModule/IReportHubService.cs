using FleetManager.Business.DataObjects.ReportsCenter;
using FleetManager.Business.UtilityModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Interfaces.ReportHubModule
{
    public interface IReportHubService
    {
        Task<DailyFleetActivityReportDto> GetDailyFleetActivityAsync(DateTime date, ReportFilter filter = null);
        Task<PaginatedResult<DriverPerformanceReportDto>> GetDriverPerformanceAsync(DateTime from, DateTime to, ReportFilter filter, int page = 1, int pageSize = 25);
        Task<FuelConsumptionReportDto> GetFuelConsumptionAsync(DateTime from, DateTime to, ReportFilter filter);
        Task<PaginatedResult<TripSummaryDto>> GetTripSummaryAsync(DateTime from, DateTime to, ReportFilter filter, int page = 1, int pageSize = 25);

        Task<CostAnalysisDto> GetCostAnalysisAsync(DateTime from, DateTime to, ReportFilter filter);
        //Task<PaginatedResult<IncidentReportDto>> GetIncidentReportAsync(DateTime from, DateTime to, ReportFilter filter, int page = 1, int pageSize = 25);
        Task<PaginatedResult<VehicleInspectionDto>> GetVehicleInspectionReportAsync(DateTime from, DateTime to, ReportFilter filter, int page = 1, int pageSize = 25);
        Task<List<DriverLicenseExpiryDto>> GetDriverLicenseExpiryAsync(DateTime from, DateTime to);
        Task<List<VehicleDocumentationDto>> GetVehicleDocumentationReportAsync();
        Task<List<VehicleUtilizationDto>> GetVehicleUtilizationAsync(DateTime from, DateTime to, ReportFilter filter);

        Task<List<VehicleComparisonDto>> GetVehicleComparisonAsync(DateTime from, DateTime to, IEnumerable<long> vehicleIds);
        Task<List<MaintenanceScheduleDto>> GetMaintenanceScheduleAsync();
        Task<List<TireManagementDto>> GetTireManagementAsync();
        Task<List<OvertimeAnalysisDto>> GetOvertimeAnalysisAsync(DateTime from, DateTime to);

        // Dashboard summary
        Task<ReportSummaryViewModel> GetDashboardSummaryAsync(DateTime from, DateTime to);

        #region Helpers
        // Services/Reports/IReportService.cs
        Task<PaginatedResult<SelectListItem>> SearchDriversAsync(string q, int page = 1, int pageSize = 20);
        Task<PaginatedResult<SelectListItem>> SearchVehiclesAsync(string q, int page = 1, int pageSize = 20);
        Task<SelectListItem?> GetDriverByIdAsync(long id);
        Task<SelectListItem?> GetVehicleByIdAsync(long id);

        // Cache invalidation (call from Driver/Vehicle CRUD services after changes)
        Task InvalidateDriverCacheAsync(long? branchId = null);
        Task InvalidateVehicleCacheAsync(long? branchId = null);

        #endregion
    }
}
