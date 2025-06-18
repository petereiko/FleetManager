using FleetManager.Business.GoogleMap.Options;
using FleetManager.Business.GoogleRoutesApi.Interfaces;
using FleetManager.Business.GoogleRoutesApi.Models;
using FleetManager.Business.ViewModels;
using FleetManager.Business.ViewModels.GoogleViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FleetManager.App.Controllers
{
    public class RoutesController : Controller
    {
        private readonly IGoogleRoutesService _routesService;
        private readonly string _apiKey;

        public RoutesController(
            IGoogleRoutesService routesService,
            IOptions<GoogleRoutesApiOptions> options)
        {
            _routesService = routesService;
            _apiKey = options.Value.ApiKey;
        }

        // GET: /Routes
        [HttpGet]
        public IActionResult Index()
        {
            // Empty VM: no response yet
            return View(new RouteViewModel());
        }

        // POST: /Routes
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(RouteViewModel vm, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return View(vm);

            try
            {
                // Build the ComputeRoutesRequest exactly to match your model
                var request = new ComputeRoutesRequest
                {
                    Origin = new Waypoint { Address = vm.OriginAddress },
                    Destination = new Waypoint { Address = vm.DestinationAddress },
                    Intermediates = new List<Waypoint>(),
                    TravelMode = "DRIVE",
                    RoutingPreference = "TRAFFIC_AWARE",
                    ComputeAlternativeRoutes = false,
                    RouteModifiers = new RouteModifiers
                    {
                        AvoidTolls = false,
                        AvoidFerries = false,
                        AvoidHighways = false
                    },   
                    LanguageCode = "en-US",
                    Units = "METRIC"
                };

                vm.Response = await _routesService
                    .ComputeRoutesAsync(request, ct);
            }
            catch (HttpRequestException)
            {
                // Redirect to a friendly error page
                return RedirectToAction("ServiceUnavailable");
            }

            return View(vm);
        }

        // GET: /Routes/Map?polyline=...
        //[HttpGet]
        //public IActionResult Map(string polyline)
        //{
        //    if (string.IsNullOrEmpty(polyline))
        //        return BadRequest();

        //    var mapVm = new MapViewModel
        //    {
        //        EncodedPolyline = polyline,
        //        ApiKey = _apiKey
        //    };
        //    return View(mapVm);
        //}



        [HttpGet]
        public IActionResult Map(string polyline, string origin = null, string destination = null)
        {
            if (string.IsNullOrEmpty(polyline))
                return BadRequest();

            var mapVm = new MapViewModel
            {
                EncodedPolyline = polyline,
                ApiKey = _apiKey,
                OriginAddress = origin,
                DestinationAddress = destination
            };

            return View(mapVm);
        }


        [HttpGet]
        public IActionResult ServiceUnavailable()
        {
            return View();  // Views/Routes/ServiceUnavailable.cshtml
        }
    }
}
