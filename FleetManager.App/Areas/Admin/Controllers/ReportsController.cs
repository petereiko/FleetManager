using FleetManager.Business.DataObjects.ReportsDto;
using FleetManager.Business.Enums;
using FleetManager.Business.Interfaces.ReportModule;
using FleetManager.Business.Interfaces.UserModule;
using Microsoft.AspNetCore.Mvc;

namespace FleetManager.App.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ReportsController : Controller
    {
        private readonly IReportService _reportService;
        private readonly IAuthUser _authUser;
        private readonly IReportExportService _exportService;
        private readonly ILogger<ReportsController> _logger;

        public ReportsController(
            IReportService reportService,
            IAuthUser authUser,
            IReportExportService exportService,
            ILogger<ReportsController> logger)
        {
            _reportService = reportService;
            _authUser = authUser;
            _exportService = exportService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var branchId = _authUser.CompanyBranchId.Value;

            var reports = await _reportService.GetReportMetadataListAsync();

            var summary = await _reportService.GetDashboardSummaryAsync(new ReportRequest
            {
                StartDate = DateTime.UtcNow.AddMonths(-1),
                EndDate = DateTime.UtcNow
            }, branchId);

            var driverSummary = summary.ContainsKey("DriverSummary") ? summary["DriverSummary"] : null;
            var vehicleSummary = summary.ContainsKey("VehicleSummary") ? summary["VehicleSummary"] : null;

            var viewModel = new ReportsCenterViewModel
            {
                MetadataList = reports,
                TotalDriverCount = driverSummary?.GetType().GetProperty("Total")?.GetValue(driverSummary) as int? ?? 0,
                ActiveDriverCount = driverSummary?.GetType().GetProperty("Active")?.GetValue(driverSummary) as int? ?? 0,
                TotalVehicleCount = vehicleSummary?.GetType().GetProperty("Total")?.GetValue(vehicleSummary) as int? ?? 0,
                ActiveVehicleCount = vehicleSummary?.GetType().GetProperty("Active")?.GetValue(vehicleSummary) as int? ?? 0,
            };

            return View(viewModel);
        }


        // GET: /Reports/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            try
            {
                var request = new ReportRequest
                {
                    StartDate = DateTime.UtcNow.AddMonths(-1),
                    EndDate = DateTime.UtcNow
                };

                var branchId = _authUser.CompanyBranchId.Value;
                var summary = await _reportService.GetDashboardSummaryAsync(request, branchId);

                var vehicleSummary = (dynamic)summary["VehicleSummary"];
                var driverSummary = (dynamic)summary["DriverSummary"];
                var fuelSummary = (dynamic)summary["FuelSummary"];
                var complianceSummary = (dynamic)summary["ComplianceSummary"];

                var viewModel = new DashboardViewModel
                {
                    TotalVehicles = vehicleSummary.Total,
                    ActiveVehicles = vehicleSummary.Active,

                    TotalDrivers = driverSummary.Total,
                    ActiveDrivers = driverSummary.Active,
                    ExpiringLicenses = driverSummary.ExpiringLicenses,

                    TotalFuelCost = fuelSummary.TotalCost,
                    TotalFuelVolume = fuelSummary.TotalVolume,
                    TotalFillUps = fuelSummary.TotalFillUps,
                    AverageFuelEfficiency = fuelSummary.TotalVolume > 0 ?
                                            Math.Round((double)(fuelSummary.TotalVolume / fuelSummary.TotalFillUps), 2) : 0,

                    ExpiredInsurance = complianceSummary.ExpiredInsurance,
                    ExpiredRoadWorthy = complianceSummary.ExpiredRoadWorthy
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard");
                TempData["ErrorMessage"] = "Failed to load dashboard data";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: /Reports/DriverPerformance
        public async Task<IActionResult> DriverPerformance(ReportRequest request)
        {
            try
            {
                var branchId = _authUser.CompanyBranchId.Value;
                var result = await _reportService.GetDriverPerformanceReportAsync(request, branchId);
                ViewBag.SummaryCards = MapSummaryToCardItems(result.Summary);
                return View(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading driver performance report");
                TempData["ErrorMessage"] = "Failed to generate report";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: /Reports/DriverCompliance
        public async Task<IActionResult> DriverCompliance(ReportRequest request)
        {
            try
            {
                var branchId = _authUser.CompanyBranchId.Value;
                var result = await _reportService.GetDriverComplianceReportAsync(request, branchId);
                return View(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading driver compliance report");
                TempData["ErrorMessage"] = "Failed to generate report";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: /Reports/VehicleUtilization
        public async Task<IActionResult> VehicleUtilization(ReportRequest request)
        {
            try
            {
                var branchId = _authUser.CompanyBranchId.Value;
                var result = await _reportService.GetVehicleUtilizationReportAsync(request, branchId);
                return View(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading vehicle utilization report");
                TempData["ErrorMessage"] = "Failed to generate report";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: /Reports/VehicleMaintenance
        public async Task<IActionResult> VehicleMaintenance(ReportRequest request)
        {
            try
            {
                var branchId = _authUser.CompanyBranchId.Value;
                var result = await _reportService.GetVehicleMaintenanceReportAsync(request, branchId);
                return View(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading vehicle maintenance report");
                TempData["ErrorMessage"] = "Failed to generate report";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: /Reports/FuelConsumption
        public async Task<IActionResult> FuelConsumption(ReportRequest request)
        {
            try
            {
                var branchId = _authUser.CompanyBranchId.Value;
                var result = await _reportService.GetFuelConsumptionReportAsync(request, branchId);
                return View(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading fuel consumption report");
                TempData["ErrorMessage"] = "Failed to generate report";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: /Reports/FuelEfficiency
        public async Task<IActionResult> FuelEfficiency(ReportRequest request)
        {
            try
            {
                var branchId = _authUser.CompanyBranchId.Value;
                var result = await _reportService.GetFuelEfficiencyReportAsync(request, branchId);
                return View(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading fuel efficiency report");
                TempData["ErrorMessage"] = "Failed to generate report";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: /Reports/VehicleAssignment
        public async Task<IActionResult> VehicleAssignment(ReportRequest request)
        {
            try
            {
                var branchId = _authUser.CompanyBranchId.Value;
                var result = await _reportService.GetVehicleAssignmentReportAsync(request, branchId);
                return View(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading vehicle assignment report");
                TempData["ErrorMessage"] = "Failed to generate report";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: /Reports/Export
        [HttpPost]
        public async Task<IActionResult> Export(ExportRequest request)
        {
            try
            {
                var branchId = _authUser.CompanyBranchId.Value;

                // Dynamically get report based on type
                dynamic reportData = request.ReportRequest.ReportType switch
                {
                    "DriverPerformance" => await _reportService.GetDriverPerformanceReportAsync(request.ReportRequest, branchId),
                    "DriverCompliance" => await _reportService.GetDriverComplianceReportAsync(request.ReportRequest, branchId),
                    "VehicleUtilization" => await _reportService.GetVehicleUtilizationReportAsync(request.ReportRequest, branchId),
                    "VehicleMaintenance" => await _reportService.GetVehicleMaintenanceReportAsync(request.ReportRequest, branchId),
                    "FuelConsumption" => await _reportService.GetFuelConsumptionReportAsync(request.ReportRequest, branchId),
                    "FuelEfficiency" => await _reportService.GetFuelEfficiencyReportAsync(request.ReportRequest, branchId),
                    "VehicleAssignment" => await _reportService.GetVehicleAssignmentReportAsync(request.ReportRequest, branchId),
                    _ => throw new ArgumentException($"Unsupported report type: {request.ReportRequest.ReportType}")
                };

                var fileBytes = await _exportService.ExportReportAsync(reportData, request.Format);

                return File(
                    fileBytes,
                    GetMimeType(request.Format),
                    $"{request.ReportRequest.ReportType}_{DateTime.Now:yyyyMMdd}.{GetFileExtension(request.Format)}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Export failed for {ReportType}", request.ReportRequest.ReportType);
                TempData["ErrorMessage"] = $"Export failed: {ex.Message}";
                return RedirectToAction(request.ReportRequest.ReportType, new { request.ReportRequest });
            }
        }

        private string GetMimeType(ExportFormat format)
        {
            return format switch
            {
                ExportFormat.Excel => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ExportFormat.PDF => "application/pdf",
                ExportFormat.CSV => "text/csv",
                _ => "application/octet-stream"
            };
        }

        private string GetFileExtension(ExportFormat format)
        {
            return format switch
            {
                ExportFormat.Excel => "xlsx",
                ExportFormat.PDF => "pdf",
                ExportFormat.CSV => "csv",
                _ => "bin"
            };
        }
        // Helper method to convert summary dictionary to strongly typed card items
        private List<SummaryCardItem> MapSummaryToCardItems(Dictionary<string, object> summary)
        {
            return new List<SummaryCardItem>
            {
                new SummaryCardItem
                {
                    Key = "Total Drivers",
                    Value = summary["TotalDrivers"],
                    IconClass = "fa-users",
                    BackgroundClass = "bg-primary"
                },
                new SummaryCardItem
                {
                    Key = "Active Drivers",
                    Value = summary["ActiveDrivers"],
                    IconClass = "fa-user-check",
                    BackgroundClass = "bg-success"
                },
                new SummaryCardItem
                {
                    Key = "Expired Licenses",
                    Value = summary["DriversWithExpiredLicense"],
                    IconClass = "fa-id-card",
                    BackgroundClass = "bg-warning"
                },
                new SummaryCardItem
                {
                    Key = "Avg. Performance Score",
                    Value = summary["AveragePerformanceScore"],
                    IconClass = "fa-chart-line",
                    BackgroundClass = "bg-info",
                    IsPercentage = true
                }
            };
        }

    }
}
