using FleetManager.Business.DataObjects.ReportsCenter;
using FleetManager.Business.Interfaces.ReportHubModule;
using FleetManager.Business.Interfaces.UserModule;
using FleetManager.Business.UtilityModels.CommonSecurity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FleetManager.App.Areas.Admin.Controllers
{
    [Area("Admin")]
    //[Authorize(Roles = "Admin")]
    public class ReportHubController : Controller
    {
        private readonly IReportHubService _reportService;
        private readonly IAuthUser _authUser;
        private readonly IIdProtector _protector;
        // If you want server-side exporter, inject it here as IReportExporter _exporter

        public ReportHubController(IReportHubService reportService, IAuthUser authUser, IIdProtector protector)
        {
            _reportService = reportService;
            _authUser = authUser;
            _protector = protector;
        }

        public async Task<IActionResult> Index(DateTime? from, DateTime? to)
        {
            var toDt = to ?? DateTime.UtcNow;
            var fromDt = from ?? toDt.AddDays(-7);
            var vm = await _reportService.GetDashboardSummaryAsync(fromDt, toDt);
            ViewBag.From = fromDt;
            ViewBag.To = toDt;
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> TripSummary(DateTime? from, DateTime? to, long? vehicleId, long? driverId, int page = 1)
        {
            var toDt = to ?? DateTime.UtcNow;
            var fromDt = from ?? toDt.AddMonths(-1);
            var filter = new ReportFilter { DriverId = driverId, VehicleId = vehicleId };
            var paged = await _reportService.GetTripSummaryAsync(fromDt, toDt, filter, page, 25);
            ViewBag.Filter = filter;
            ViewBag.From = fromDt;
            ViewBag.To = toDt;
            return View(paged);
        }

        [HttpGet]
        public async Task<IActionResult> DriverPerformance(DateTime? from, DateTime? to, long? driverId, int page = 1)
        {
            var toDt = to ?? DateTime.UtcNow;
            var fromDt = from ?? toDt.AddMonths(-1);
            var filter = new ReportFilter { DriverId = driverId };
            var paged = await _reportService.GetDriverPerformanceAsync(fromDt, toDt, filter, page, 25);
            ViewBag.From = fromDt;
            ViewBag.To = toDt;
            return View(paged);
        }

        [HttpGet]
        public async Task<IActionResult> FuelConsumption(DateTime? from, DateTime? to, long? vehicleId)
        {
            var toDt = to ?? DateTime.UtcNow;
            var fromDt = from ?? toDt.AddMonths(-1);
            var filter = new ReportFilter { VehicleId = vehicleId };
            var vm = await _reportService.GetFuelConsumptionAsync(fromDt, toDt, filter);
            ViewBag.From = fromDt;
            ViewBag.To = toDt;
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> DailyFleetActivity(DateTime? date)
        {
            var d = date ?? DateTime.UtcNow.Date;
            var vm = await _reportService.GetDailyFleetActivityAsync(d, null);
            ViewBag.Date = d;
            return View(vm);
        }

        //[HttpGet]
        //public async Task<IActionResult> IncidentReport(DateTime? from, DateTime? to, long? vehicleId, long? driverId, int page = 1)
        //{
        //    var toDt = to ?? DateTime.UtcNow;
        //    var fromDt = from ?? toDt.AddMonths(-1);
        //    var filter = new ReportFilter { VehicleId = vehicleId, DriverId = driverId };
        //    var paged = await _reportService.GetIncidentReportAsync(fromDt, toDt, filter, page, 25);
        //    ViewBag.From = fromDt;
        //    ViewBag.To = toDt;
        //    return View(paged);
        //}

        [HttpGet]
        public async Task<IActionResult> LicenseExpiry(DateTime? from, DateTime? to)
        {
            var toDt = to ?? DateTime.UtcNow.AddMonths(3);
            var fromDt = from ?? DateTime.UtcNow;
            var list = await _reportService.GetDriverLicenseExpiryAsync(fromDt, toDt);
            ViewBag.From = fromDt;
            ViewBag.To = toDt;
            return View(list);
        }

        [HttpGet]
        public async Task<IActionResult> VehicleDocs()
        {
            var list = await _reportService.GetVehicleDocumentationReportAsync();
            return View(list);
        }

        [HttpGet]
        public async Task<IActionResult> VehicleUtilization(DateTime? from, DateTime? to)
        {
            var toDt = to ?? DateTime.UtcNow;
            var fromDt = from ?? toDt.AddMonths(-1);
            var list = await _reportService.GetVehicleUtilizationAsync(fromDt, toDt, null);
            ViewBag.From = fromDt;
            ViewBag.To = toDt;
            return View(list);
        }

        [HttpGet]
        public async Task<IActionResult> VehicleComparison(DateTime? from, DateTime? to, string vehicleIdsCsv = null)
        {
            var toDt = to ?? DateTime.UtcNow;
            var fromDt = from ?? toDt.AddMonths(-1);
            var ids = string.IsNullOrWhiteSpace(vehicleIdsCsv) ? Enumerable.Empty<long>() : vehicleIdsCsv.Split(',').Select(x => long.Parse(x));
            var list = await _reportService.GetVehicleComparisonAsync(fromDt, toDt, ids);
            ViewBag.From = fromDt;
            ViewBag.To = toDt;
            return View(list);
        }

        [HttpGet]
        public async Task<IActionResult> MaintenanceSchedule()
        {
            var list = await _reportService.GetMaintenanceScheduleAsync();
            return View(list);
        }

        [HttpGet]
        public async Task<IActionResult> TireManagement()
        {
            var list = await _reportService.GetTireManagementAsync();
            return View(list);
        }

        [HttpGet]
        public async Task<IActionResult> OvertimeAnalysis(DateTime? from, DateTime? to)
        {
            var toDt = to ?? DateTime.UtcNow;
            var fromDt = from ?? toDt.AddMonths(-1);
            var list = await _reportService.GetOvertimeAnalysisAsync(fromDt, toDt);
            ViewBag.From = fromDt;
            ViewBag.To = toDt;
            return View(list);
        }


        #region Dropdwons
        // Areas/Admin/Controllers/ReportsController.cs
        [HttpGet]
        public async Task<IActionResult> SearchDrivers(string q = "", int page = 1)
        {
            const int pageSize = 20;
            var paged = await _reportService.SearchDriversAsync(q, page, pageSize);

            var results = paged.Items.Select(i => new { id = _protector.ProtectId(long.Parse(i.Value)), text = i.Text });
            return Ok(new
            {
                results,
                pagination = new { more = paged.TotalPages > page }
            });
        }

        [HttpGet]
        public async Task<IActionResult> SearchVehicles(string q = "", int page = 1)
        {
            const int pageSize = 20;
            var paged = await _reportService.SearchVehiclesAsync(q, page, pageSize);

            var results = paged.Items.Select(i => new { id = _protector.ProtectId(long.Parse(i.Value)), text = i.Text }).ToList();
            return Ok(new
            {
                results,
                pagination = new { more = paged.TotalPages > page }
            });
        }



        // Single item endpoints for initial selection

        [HttpGet]
        public async Task<IActionResult> GetDriverById(long id) // id is raw because UnprotectIdActionFilter runs
        {
            var item = await _reportService.GetDriverByIdAsync(id);
            if (item == null) return NotFound();

            return Ok(new
            {
                id = _protector.ProtectId(id), // protected id
                text = item.Text
            });
        }
        //[HttpGet]
        //public async Task<IActionResult> GetDriverById(long id)
        //{
        //    var item = await _reportService.GetDriverByIdAsync(id);
        //    if (item == null) return NotFound();
        //    return Ok(new { id = item.Value, text = item.Text });
        //}

        [HttpGet]
        public async Task<IActionResult> GetVehicleById(long id) // id is raw because UnprotectIdActionFilter runs
        {
            var dto = await _reportService.GetVehicleByIdAsync(id);
            if (dto == null) return NotFound();

            return Ok(new
            {
                id = _protector.ProtectId(id), // protected id
                text = dto.Text
            });
        }

        //[HttpGet]
        //public async Task<IActionResult> GetVehicleById(long id)
        //{
        //    var item = await _reportService.GetVehicleByIdAsync(id);
        //    if (item == null) return NotFound();

        //    return Ok(new { id = item.Value, text = item.Text });
        //}


        #endregion

        // Optional server-side export endpoints (if you want server generated files)
        // Otherwise your client-side TableExporter will handle Excel/PDF/CSV using the exportable table in views.
    }
}
