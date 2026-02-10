using FleetManager.Business.DataObjects.TripsDto;
using FleetManager.Business.Enums;
using FleetManager.Business.Interfaces.TripLocationModule;
using FleetManager.Business.Interfaces.TripModule;
using FleetManager.Business.ViewModels.TripsViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FleetManager.App.Areas.Admin.Controllers
{
    //[Route("admin/tracking")]
    [Authorize]
    [Area("Admin")]
    public class LiveTrackingController : Controller
    {
        private readonly ITripService _tripService;
        private readonly ITripLocationService _locationService;
        private readonly ILogger<LiveTrackingController> _logger;

        public LiveTrackingController(
            ITripService tripService,
            ITripLocationService locationService,
            ILogger<LiveTrackingController> logger)
        {
            _tripService = tripService;
            _locationService = locationService;
            _logger = logger;
        }

        /// <summary>
        /// Main live tracking dashboard - shows all active trips
        /// </summary>
        //[HttpGet("dashboard")]
        public async Task<IActionResult> Dashboard()
        {
            try
            {
                var filterDto = new TripFilterDto
                {
                    Status = TripStatus.InProgress,
                    Page = 1,
                    PageSize = 100
                };

                var response = await _tripService.GetTripsAsync(filterDto);

                var viewModel = new LiveTrackingDashboardViewModel
                {
                    ActiveTrips = response.Result?.Items ?? new List<TripListDto>(),
                    TotalActiveTrips = response.Result?.TotalCount ?? 0,
                    LastUpdated = DateTime.UtcNow
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading live tracking dashboard");
                return View("Error");
            }
        }

        /// <summary>
        /// Single trip live tracking view - professional Uber/Lyft style interface
        /// </summary>
        [HttpGet("trip/{tripId}")]
        public async Task<IActionResult> TrackTrip(long tripId)
        {
            try
            {
                var tripResponse = await _tripService.GetTripByIdAsync(tripId);

                if (!tripResponse.Success)
                {
                    TempData["Error"] = "Trip not found";
                    return RedirectToAction("Dashboard");
                }

                var trip = tripResponse.Result;
                var latestLocation = await _locationService.GetLatestLocationAsync(tripId);

                var viewModel = new TripTrackingViewModel
                {
                    Trip = trip,
                    LatestLocation = latestLocation,
                    IsActive = trip.Status == TripStatus.InProgress
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading trip tracking for trip {TripId}", tripId);
                return View("Error");
            }
        }

        /// <summary>
        /// API endpoint to get active trips for tracking selection
        /// </summary>
        [HttpGet("api/active-trips")]
        [Produces("application/json")]
        public async Task<IActionResult> GetActiveTrips()
        {
            try
            {
                var filterDto = new TripFilterDto
                {
                    Status = TripStatus.InProgress,
                    Page = 1,
                    PageSize = 100
                };

                var response = await _tripService.GetTripsAsync(filterDto);

                if (!response.Success)
                {
                    return Json(new { success = false, message = response.Message });
                }

                var trips = response.Result.Items.Select(t => new
                {
                    tripId = t.Id,
                    tripNumber = t.TripNumber,
                    vehiclePlateNo = t.VehiclePlateNo,
                    driverName = t.DriverName,
                    origin = t.Origin,
                    destination = t.Destination,
                    scheduledStart = t.ScheduledStartDate,
                    actualStart = t.ActualStartDate,
                    estimatedDistance = t.EstimatedDistance
                }).ToList();

                return Json(new
                {
                    success = true,
                    data = trips,
                    count = trips.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active trips");
                return Json(new { success = false, message = "Error loading active trips" });
            }
        }

        /// <summary>
        /// API endpoint to get trip route history
        /// </summary>
        [HttpGet("api/trip/{tripId}/route")]
        [Produces("application/json")]
        public async Task<IActionResult> GetTripRoute(long tripId)
        {
            try
            {
                var locations = await _locationService.GetTripLocationsAsync(tripId);

                return Json(new
                {
                    success = true,
                    tripId = tripId,
                    locations = locations,
                    count = locations.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting route for trip {TripId}", tripId);
                return Json(new { success = false, message = "Error loading route" });
            }
        }

        /// <summary>
        /// API endpoint to get latest location for a trip
        /// </summary>
        [HttpGet("api/trip/{tripId}/latest")]
        [Produces("application/json")]
        public async Task<IActionResult> GetLatestLocation(long tripId)
        {
            try
            {
                var location = await _locationService.GetLatestLocationAsync(tripId);

                if (location == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "No location data available"
                    });
                }

                return Json(new
                {
                    success = true,
                    data = location
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting latest location for trip {TripId}", tripId);
                return Json(new { success = false, message = "Error loading location" });
            }
        }
    }

}
