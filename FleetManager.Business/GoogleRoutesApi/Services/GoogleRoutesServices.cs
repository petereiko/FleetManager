using FleetManager.Business.GoogleRoutesApi.Interfaces;
using FleetManager.Business.GoogleRoutesApi.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;

namespace FleetManager.Business.GoogleRoutesApi.Services
{
    public class GoogleRoutesService : IGoogleRoutesService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<GoogleRoutesService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public GoogleRoutesService(HttpClient httpClient, ILogger<GoogleRoutesService> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
        }

        public async Task<DirectionsResponse> ComputeRoutesAsync(ComputeRoutesRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (request.Origin?.Location?.LatLng == null && string.IsNullOrEmpty(request.Origin?.Address))
                throw new ArgumentException("Origin must have either coordinates or address.", nameof(request));

            if (request.Destination?.Location?.LatLng == null && string.IsNullOrEmpty(request.Destination?.Address))
                throw new ArgumentException("Destination must have either coordinates or address.", nameof(request));

            try
            {
                var json = JsonSerializer.Serialize(request, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("", content, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError("Google Routes API failed with status {StatusCode}: {ErrorContent}", response.StatusCode, errorContent);
                    throw new HttpRequestException($"Google Routes API failed: {response.StatusCode}");
                }

                var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
                var result = await JsonSerializer.DeserializeAsync<DirectionsResponse>(responseStream, _jsonOptions, cancellationToken);

                return result ?? new DirectionsResponse { Routes = new List<Route>() };
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                // The token wasn't cancelled by the caller, so this is almost certainly a timeout
                _logger.LogError(ex, "Google Routes API request timed out after {Timeout}ms",
                                 _httpClient.Timeout.TotalMilliseconds);
                throw new TimeoutException("The request to Google Routes API timed out.", ex);
            }
            catch (TaskCanceledException)
            {
                // The caller cancelled via the CancellationToken
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while calling Google Routes API");
                throw;
            }
        }
    }
}
