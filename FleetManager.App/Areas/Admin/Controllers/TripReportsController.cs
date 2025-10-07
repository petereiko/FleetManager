using FleetManager.Business.Interfaces.TripReportModule;
using Microsoft.AspNetCore.Mvc;

namespace FleetManager.App.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class TripReportsController : Controller
    {
        private readonly ITripReportService _reportService;

        public TripReportsController(ITripReportService reportService)
        {
            _reportService = reportService;
        }

        public IActionResult Index()
        {
            return View(); // dashboard with charts (client will call JSON endpoints)
        }

        // Alternate server-side index (tables & paging)
        public IActionResult IndexServer()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetDailySummary(DateTime? start = null, DateTime? end = null)
        {
            var s = start ?? DateTime.UtcNow.AddDays(-7);
            var e = end ?? DateTime.UtcNow;
            var res = await _reportService.GetDailySummaryAsync(s, e);
            return Json(res);
        }

        [HttpGet]
        public async Task<IActionResult> GetDistanceByVehicle(DateTime? start = null, DateTime? end = null)
        {
            var s = start ?? DateTime.UtcNow.AddMonths(-1);
            var e = end ?? DateTime.UtcNow;
            var res = await _reportService.GetDistanceByVehicleAsync(s, e);
            return Json(res);
        }

        [HttpGet]
        public async Task<IActionResult> GetDistanceByDriver(DateTime? start = null, DateTime? end = null)
        {
            var s = start ?? DateTime.UtcNow.AddMonths(-1);
            var e = end ?? DateTime.UtcNow;
            var res = await _reportService.GetDistanceByDriverAsync(s, e);
            return Json(res);
        }

        [HttpGet]
        public async Task<IActionResult> GetFuelPerTrip(DateTime? start = null, DateTime? end = null)
        {
            var s = start ?? DateTime.UtcNow.AddMonths(-1);
            var e = end ?? DateTime.UtcNow;
            var res = await _reportService.GetFuelConsumptionPerTripAsync(s, e);
            return Json(res);
        }

        [HttpGet]
        public async Task<IActionResult> GetTopDrivers(DateTime? start = null, DateTime? end = null, int topN = 10)
        {
            var s = start ?? DateTime.UtcNow.AddMonths(-1);
            var e = end ?? DateTime.UtcNow;
            var res = await _reportService.GetTopDriversAsync(s, e, topN);
            return Json(res);
        }

        [HttpGet]
        public async Task<IActionResult> GetVehicleUtilization(DateTime? start = null, DateTime? end = null)
        {
            var s = start ?? DateTime.UtcNow.AddMonths(-1);
            var e = end ?? DateTime.UtcNow;
            var res = await _reportService.GetVehicleUtilizationAsync(s, e);
            return Json(res);
        }

        // drilldown: trips for vehicle (server-side paging)
        [HttpGet]
        public async Task<IActionResult> GetTripsForVehicle(long vehicleId, int page = 1, int pageSize = 20)
        {
            var res = await _reportService.GetTripsForVehicleAsync(vehicleId, page, pageSize);
            return Json(res);
        }

        // route replay checkpoints
        [HttpGet]
        public async Task<IActionResult> GetTripCheckpoints(long tripId)
        {
            var res = await _reportService.GetCheckpointsForTripAsync(tripId);
            return Json(res);
        }
    }
}
