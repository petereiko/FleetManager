using FleetManager.Business.DataObjects.ApiModels;
using FleetManager.Business.DataObjects.ApiModels.GoogleRoutes;
using FleetManager.Business.GoogleMap.Options;
using FleetManager.Business.GoogleRoutesApi.Interfaces;
using FleetManager.Business.GoogleRoutesApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FleetManager.App.Controllers
{
    [Route("api/driver/routes")]
    [ApiController]
    [Authorize(Policy = "DriverApi")]
    public class DriverRoutesApiController : ControllerBase
    {
        private readonly IGoogleRoutesService _routesService;
        private readonly ILogger<DriverRoutesApiController> _logger;
        private readonly GoogleRoutesApiOptions _googleOptions;

        public DriverRoutesApiController(
            IGoogleRoutesService routesService,
            ILogger<DriverRoutesApiController> logger,
            IOptions<GoogleRoutesApiOptions> googleOptions)
        {
            _routesService = routesService;
            _logger = logger;
            _googleOptions = googleOptions.Value;
        }

        /// <summary>
        /// Get route directions using addresses
        /// Returns multiple route options with distance, duration, and polyline
        /// </summary>
        [HttpPost("by-address")]
        [ProducesResponseType(typeof(ApiResponse<RouteResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        public async Task<IActionResult> GetRoutesByAddress([FromBody] RouteRequest request, CancellationToken ct = default)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    return BadRequest(new ApiResponse<RouteResponse>
                    {
                        Success = false,
                        Message = "Validation failed",
                        Errors = errors
                    });
                }

                // Build intermediates
                var intermediates = new List<Waypoint>();
                if (request.IntermediateAddresses?.Any() == true)
                {
                    intermediates = request.IntermediateAddresses
                        .Select(addr => new Waypoint { Address = addr })
                        .ToList();
                }

                // Build Google Routes API request
                var googleRequest = new ComputeRoutesRequest
                {
                    Origin = new Waypoint { Address = request.OriginAddress },
                    Destination = new Waypoint { Address = request.DestinationAddress },
                    Intermediates = intermediates,
                    TravelMode = request.TravelMode.ToUpper(),
                    RoutingPreference = request.RoutingPreference.ToUpper(),
                    ComputeAlternativeRoutes = request.ComputeAlternativeRoutes,
                    RouteModifiers = new RouteModifiers
                    {
                        AvoidTolls = request.AvoidTolls,
                        AvoidHighways = request.AvoidHighways,
                        AvoidFerries = request.AvoidFerries
                    },
                    LanguageCode = "en-US",
                    Units = request.Units.ToUpper()
                };

                var googleResponse = await _routesService.ComputeRoutesAsync(googleRequest, ct);

                if (googleResponse?.Routes == null || !googleResponse.Routes.Any())
                {
                    return Ok(new ApiResponse<RouteResponse>
                    {
                        Success = false,
                        Message = "No routes found between the specified locations",
                        Data = new RouteResponse { Status = "NO_ROUTES_FOUND" }
                    });
                }

                // Map to API response
                var response = MapToRouteResponse(googleResponse);

                return Ok(new ApiResponse<RouteResponse>
                {
                    Success = true,
                    Message = $"Found {response.Routes.Count} route option(s)",
                    Data = response
                });
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Google Routes API service unavailable");
                return StatusCode(503, new ApiResponse<RouteResponse>
                {
                    Success = false,
                    Message = "Route service is temporarily unavailable. Please try again later."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error computing routes by address");
                return StatusCode(500, new ApiResponse<RouteResponse>
                {
                    Success = false,
                    Message = "An error occurred while computing routes"
                });
            }
        }

        /// <summary>
        /// Get route directions using GPS coordinates
        /// Better for mobile apps that have current location
        /// </summary>
        [HttpPost("by-coordinates")]
        [ProducesResponseType(typeof(ApiResponse<RouteResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetRoutesByCoordinates([FromBody] RouteCoordinateRequest request, CancellationToken ct = default)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    return BadRequest(new ApiResponse<RouteResponse>
                    {
                        Success = false,
                        Message = "Validation failed",
                        Errors = errors
                    });
                }

                // Build intermediates
                var intermediates = new List<Waypoint>();
                if (request.IntermediatePoints?.Any() == true)
                {
                    intermediates = request.IntermediatePoints
                        .Select(point => new Waypoint
                        {
                            Location = new Location
                            {
                                LatLng = new LatLng
                                {
                                    Latitude = point.Latitude,
                                    Longitude = point.Longitude
                                }
                            }
                        })
                        .ToList();
                }

                // Build Google Routes API request
                var googleRequest = new ComputeRoutesRequest
                {
                    Origin = new Waypoint
                    {
                        Location = new Location
                        {
                            LatLng = new LatLng
                            {
                                Latitude = request.OriginLatitude,
                                Longitude = request.OriginLongitude
                            }
                        }
                    },
                    Destination = new Waypoint
                    {
                        Location = new Location
                        {
                            LatLng = new LatLng
                            {
                                Latitude = request.DestinationLatitude,
                                Longitude = request.DestinationLongitude
                            }
                        }
                    },
                    Intermediates = intermediates,
                    TravelMode = request.TravelMode.ToUpper(),
                    RoutingPreference = request.RoutingPreference.ToUpper(),
                    ComputeAlternativeRoutes = request.ComputeAlternativeRoutes,
                    RouteModifiers = new RouteModifiers
                    {
                        AvoidTolls = request.AvoidTolls,
                        AvoidHighways = request.AvoidHighways,
                        AvoidFerries = request.AvoidFerries
                    },
                    LanguageCode = "en-US",
                    Units = "METRIC"
                };

                var googleResponse = await _routesService.ComputeRoutesAsync(googleRequest, ct);

                if (googleResponse?.Routes == null || !googleResponse.Routes.Any())
                {
                    return Ok(new ApiResponse<RouteResponse>
                    {
                        Success = false,
                        Message = "No routes found between the specified coordinates",
                        Data = new RouteResponse { Status = "NO_ROUTES_FOUND" }
                    });
                }

                // Map to API response
                var response = MapToRouteResponse(googleResponse);

                return Ok(new ApiResponse<RouteResponse>
                {
                    Success = true,
                    Message = $"Found {response.Routes.Count} route option(s)",
                    Data = response
                });
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Google Routes API service unavailable");
                return StatusCode(503, new ApiResponse<RouteResponse>
                {
                    Success = false,
                    Message = "Route service is temporarily unavailable. Please try again later."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error computing routes by coordinates");
                return StatusCode(500, new ApiResponse<RouteResponse>
                {
                    Success = false,
                    Message = "An error occurred while computing routes"
                });
            }
        }

        /// <summary>
        /// Get travel mode options for dropdown
        /// </summary>
        [HttpGet("travel-modes")]
        [ProducesResponseType(typeof(ApiResponse<List<TravelModeOption>>), StatusCodes.Status200OK)]
        public IActionResult GetTravelModes()
        {
            var travelModes = new List<TravelModeOption>
            {
                new TravelModeOption { Value = "DRIVE", Text = "Driving", Icon = "🚗" },
                new TravelModeOption { Value = "BICYCLE", Text = "Bicycle", Icon = "🚴" },
                new TravelModeOption { Value = "WALK", Text = "Walking", Icon = "🚶" },
                new TravelModeOption { Value = "TWO_WHEELER", Text = "Motorcycle", Icon = "🏍️" }
            };

            return Ok(new ApiResponse<List<TravelModeOption>>
            {
                Success = true,
                Message = "Travel modes retrieved",
                Data = travelModes
            });
        }

        /// <summary>
        /// Get routing preference options for dropdown
        /// </summary>
        [HttpGet("routing-preferences")]
        [ProducesResponseType(typeof(ApiResponse<List<RoutingPreferenceOption>>), StatusCodes.Status200OK)]
        public IActionResult GetRoutingPreferences()
        {
            var preferences = new List<RoutingPreferenceOption>
            {
                new RoutingPreferenceOption
                {
                    Value = "TRAFFIC_AWARE",
                    Text = "Traffic Aware",
                    Description = "Considers current traffic conditions"
                },
                new RoutingPreferenceOption
                {
                    Value = "TRAFFIC_AWARE_OPTIMAL",
                    Text = "Traffic Optimal",
                    Description = "Optimizes for current and predicted traffic"
                },
                new RoutingPreferenceOption
                {
                    Value = "TRAFFIC_UNAWARE",
                    Text = "Fastest Route",
                    Description = "Ignores traffic conditions"
                }
            };

            return Ok(new ApiResponse<List<RoutingPreferenceOption>>
            {
                Success = true,
                Message = "Routing preferences retrieved",
                Data = preferences
            });
        }

        #region Helper Methods

        private RouteResponse MapToRouteResponse(DirectionsResponse googleResponse)
        {
            var response = new RouteResponse
            {
                Status = "OK",
                Routes = googleResponse.Routes.Select(r => new RouteOption
                {
                    RouteLabels = r.RouteLabels ?? new List<string>(),
                    DistanceMeters = r.DistanceMeters,
                    DistanceText = FormatDistance(r.DistanceMeters),
                    Duration = r.Duration,
                    DurationMinutes = ParseDurationToMinutes(r.Duration),
                    DurationText = FormatDuration(r.Duration),
                    EncodedPolyline = r.Polyline?.EncodedPolyline ?? string.Empty,
                    Warnings = r.Warnings ?? new List<string>(),
                    HasTolls = r.TravelAdvisory?.TollInfo != null,
                    TollInfo = r.TravelAdvisory?.TollInfo != null
                        ? new TollInformation
                        {
                            EstimatedPrice = r.TravelAdvisory.TollInfo.EstimatedPrice ?? new List<string>()
                        }
                        : null,
                    RouteToken = r.RouteToken ?? string.Empty,
                    Legs = r.Legs?.Select(leg => new RouteLeg
                    {
                        DistanceMeters = leg.DistanceMeters,
                        Duration = leg.Duration,
                        StartLocation = new RouteLocation
                        {
                            Latitude = leg.StartLocation?.LatLng?.Latitude ?? 0,
                            Longitude = leg.StartLocation?.LatLng?.Longitude ?? 0
                        },
                        EndLocation = new RouteLocation
                        {
                            Latitude = leg.EndLocation?.LatLng?.Latitude ?? 0,
                            Longitude = leg.EndLocation?.LatLng?.Longitude ?? 0
                        },
                        Steps = leg.Steps?.Select(step => new RouteStep
                        {
                            DistanceMeters = step.DistanceMeters,
                            Duration = step.Duration,
                            Instruction = step.NavigationInstruction?.Instructions ?? string.Empty,
                            StartLocation = new RouteLocation
                            {
                                Latitude = step.StartLocation?.LatLng?.Latitude ?? 0,
                                Longitude = step.StartLocation?.LatLng?.Longitude ?? 0
                            },
                            EndLocation = new RouteLocation
                            {
                                Latitude = step.EndLocation?.LatLng?.Latitude ?? 0,
                                Longitude = step.EndLocation?.LatLng?.Longitude ?? 0
                            }
                        }).ToList() ?? new List<RouteStep>()
                    }).ToList() ?? new List<RouteLeg>()
                }).ToList()
            };

            return response;
        }

        private string FormatDistance(int meters)
        {
            if (meters < 1000)
            {
                return $"{meters} m";
            }

            var km = meters / 1000.0;
            return $"{km:F1} km";
        }

        private int ParseDurationToMinutes(string duration)
        {
            // Duration format: "2345s"
            if (string.IsNullOrEmpty(duration)) return 0;

            var secondsStr = duration.Replace("s", "");
            if (int.TryParse(secondsStr, out var seconds))
            {
                return (int)Math.Ceiling(seconds / 60.0);
            }

            return 0;
        }

        private string FormatDuration(string duration)
        {
            var minutes = ParseDurationToMinutes(duration);

            if (minutes < 60)
            {
                return $"{minutes} min{(minutes != 1 ? "s" : "")}";
            }

            var hours = minutes / 60;
            var remainingMinutes = minutes % 60;

            if (remainingMinutes == 0)
            {
                return $"{hours} hr{(hours != 1 ? "s" : "")}";
            }

            return $"{hours} hr{(hours != 1 ? "s" : "")} {remainingMinutes} min{(remainingMinutes != 1 ? "s" : "")}";
        }

        #endregion
    }

    
}
