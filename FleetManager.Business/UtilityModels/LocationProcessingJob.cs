using FleetManager.Business.Interfaces.TripLocationModule;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.UtilityModels
{
    public class LocationProcessingJob
    {
        private readonly ITripLocationService _locationService;
        private readonly ILogger<LocationProcessingJob> _logger;

        public LocationProcessingJob(
            ITripLocationService locationService,
            ILogger<LocationProcessingJob> logger)
        {
            _locationService = locationService;
            _logger = logger;
        }

        public async Task ProcessLocations()
        {
            _logger.LogInformation("Background location processing job started");
            await _locationService.ProcessLocationQueueAsync();
        }
    }
}
