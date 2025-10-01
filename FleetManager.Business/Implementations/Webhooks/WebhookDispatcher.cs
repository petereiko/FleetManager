using FleetManager.Business.Database.Entities;
using FleetManager.Business.Interfaces.WebhookModule;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace FleetManager.Business.Implementations.Webhooks
{
    public class WebhookDispatcher : IWebhookDispatcher
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<WebhookDispatcher> _logger;
        private readonly FleetManagerDbContext _dbContext;
        private readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy;
        private readonly string _globalSecret;

        public WebhookDispatcher(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<WebhookDispatcher> logger, FleetManagerDbContext dbContext)
        {
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

            _globalSecret = _configuration["TripWebhooks:SecretKey"] ?? string.Empty;

            _retryPolicy = Policy<HttpResponseMessage>
                .Handle<HttpRequestException>()
                .OrResult(r => !r.IsSuccessStatusCode)
                .WaitAndRetryAsync(new[]
                {
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(5),
                    TimeSpan.FromSeconds(15)
                }, (outcome, timespan, retryCount, context) =>
                {
                    var reason = outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString();
                    _logger.LogWarning("Webhook dispatch retry {Retry} after {Delay} due to {Reason}", retryCount, timespan, reason);
                });
        }

        public async Task DispatchAsync(string eventName, long entityId, object payload, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(eventName)) throw new ArgumentNullException(nameof(eventName));

            string configKey = eventName switch
            {
                "TripStarted" => "TripWebhooks:OnStart",
                "TripCompleted" => "TripWebhooks:OnComplete",
                _ => null
            };

            if (string.IsNullOrWhiteSpace(configKey)) return;

            var urlsStr = _configuration[configKey];
            if (string.IsNullOrWhiteSpace(urlsStr)) return;

            var urls = urlsStr
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(u => u.Trim())
                .Where(u => Uri.IsWellFormedUriString(u, UriKind.Absolute))
                .ToList();

            if (!urls.Any()) return;

            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            foreach (var url in urls)
            {
                var log = new WebhookDeliveryLog
                {
                    EventName = eventName,
                    EntityId = entityId,
                    Url = url,
                    Payload = json,
                    AttemptCount = 0,
                    CreatedAt = DateTime.UtcNow,
                    Succeeded = false
                };

                _dbContext.WebhookDeliveryLogs.Add(log);
                await _dbContext.SaveChangesAsync(cancellationToken);

                try
                {
                    var client = _httpClientFactory.CreateClient();

                    var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
                    var signature = ComputeSignature(json, timestamp, _globalSecret);

                    using var request = new HttpRequestMessage(HttpMethod.Post, url)
                    {
                        Content = new StringContent(json, Encoding.UTF8, "application/json")
                    };

                    request.Headers.Add("X-Event", eventName);
                    request.Headers.Add("X-Timestamp", timestamp);
                    request.Headers.Add("X-Signature", $"sha256={signature}");

                    var response = await _retryPolicy.ExecuteAsync(ct => client.SendAsync(request, ct), cancellationToken);

                    log.AttemptCount += 1;
                    log.LastAttemptedAt = DateTime.UtcNow;

                    if (response.IsSuccessStatusCode)
                    {
                        log.Succeeded = true;
                        await _dbContext.SaveChangesAsync(cancellationToken);
                        _logger.LogInformation("Webhook delivered to {Url} for event {Event}", url, eventName);
                    }
                    else
                    {
                        var body = await response.Content.ReadAsStringAsync(cancellationToken);
                        log.LastError = $"Status {response.StatusCode} - {Truncate(body, 2000)}";
                        await _dbContext.SaveChangesAsync(cancellationToken);
                        _logger.LogWarning("Webhook to {Url} returned {Status} for event {Event}", url, response.StatusCode, eventName);
                    }
                }
                catch (Exception ex)
                {
                    log.AttemptCount += 1;
                    log.LastAttemptedAt = DateTime.UtcNow;
                    log.LastError = Truncate(ex.ToString(), 2000);
                    await _dbContext.SaveChangesAsync(cancellationToken);
                    _logger.LogWarning(ex, "Error dispatching webhook to {Url} for event {Event}", url, eventName);
                }
            }
        }

        private static string ComputeSignature(string payload, string timestamp, string secret)
        {
            var message = (timestamp ?? string.Empty) + "." + (payload ?? string.Empty);
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret ?? string.Empty));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        private static string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }
    }
}
