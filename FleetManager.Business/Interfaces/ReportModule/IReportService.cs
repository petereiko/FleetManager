using FleetManager.Business.DataObjects.ReportsDto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.Interfaces.ReportModule
{
    public interface IReportService
    {
        // Driver Reports
        Task<ReportResult<DriverPerformanceReportDto>> GetDriverPerformanceReportAsync(ReportRequest request, long branchId);
        Task<ReportResult<DriverComplianceReportDto>> GetDriverComplianceReportAsync(ReportRequest request, long branchId);

        // Vehicle Reports
        Task<ReportResult<VehicleUtilizationReportDto>> GetVehicleUtilizationReportAsync(ReportRequest request, long branchId);
        Task<ReportResult<VehicleMaintenanceReportDto>> GetVehicleMaintenanceReportAsync(ReportRequest request, long branchId);

        // Fuel Reports
        Task<ReportResult<FuelConsumptionReportDto>> GetFuelConsumptionReportAsync(ReportRequest request, long branchId);
        Task<ReportResult<FuelEfficiencyReportDto>> GetFuelEfficiencyReportAsync(ReportRequest request, long branchId);

        // Assignment Reports
        Task<ReportResult<VehicleAssignmentReportDto>> GetVehicleAssignmentReportAsync(ReportRequest request, long branchId);

        // Dashboard Summary
        Task<Dictionary<string, object>> GetDashboardSummaryAsync(ReportRequest request, long branchId);
        Task<List<ReportMetadata>> GetReportMetadataListAsync();

    }

}
