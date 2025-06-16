using FleetManager.Business.GoogleRoutesApi.Interfaces;
using FleetManager.Business.GoogleRoutesApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace FleetManager.Business.GoogleRoutesApi.Services
{
    public class FakeRoutesService : IGoogleRoutesService
    {
        private readonly string _sampleJsonPath;

        public FakeRoutesService(IHostEnvironment env)
        {
            // ContentRootPath points at your project root (where appsettings.json lives)
            _sampleJsonPath = Path.Combine(env.ContentRootPath, "Mocks", "sample.json");
        }

        public Task<DirectionsResponse> ComputeRoutesAsync(
            ComputeRoutesRequest request,
            CancellationToken cancellationToken = default)
        {
            // Read the file that was copied into bin/.../Mocks
            var json = File.ReadAllText(_sampleJsonPath);

            var response = JsonSerializer.Deserialize<DirectionsResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            })!;

            return Task.FromResult(response);
        }
    }

}
