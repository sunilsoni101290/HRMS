using System.Net.Http.Json;
using BiometricAgent.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BiometricAgent.Services
{
    public class ApiClient
    {
        private readonly HttpClient _http;
        private readonly AgentOptions _options;
        private readonly ILogger<ApiClient> _logger;

        public ApiClient(HttpClient http, IOptions<AgentOptions> options, ILogger<ApiClient> logger)
        {
            _http = http;
            _options = options.Value;
            _http.BaseAddress = new Uri(_options.ApiBaseUrl.TrimEnd('/') + "/");
            _logger = logger;
        }

        /// <summary>
        /// Pushes a batch of punches to POST /api/BiometricSync/ingest.
        /// Returns true only on a confirmed 200 OK from the server - callers
        /// should keep the batch queued locally on any other outcome
        /// (network failure, 401 bad device key, 5xx, etc.).
        /// </summary>
        public async Task<bool> PushPunchesAsync(List<PunchRecord> punches, CancellationToken ct)
        {
            if (punches.Count == 0)
                return true;

            var payload = new
            {
                DeviceCode = _options.Device.DeviceCode,
                DeviceKey = _options.Device.DeviceKey,
                Punches = punches.Select(p => new
                {
                    p.EmployeeCode,
                    p.PunchTime,
                    PunchType = (int)p.PunchType
                })
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, "api/BiometricSync/ingest")
            {
                Content = JsonContent.Create(payload)
            };

            request.Headers.Add("X-Tenant-ID", _options.TenantId);

            try
            {
                using var response = await _http.SendAsync(request, ct);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Pushed {Count} punch(es) to the ERP API.", punches.Count);
                    return true;
                }

                var body = await response.Content.ReadAsStringAsync(ct);

                _logger.LogWarning(
                    "ERP API rejected the punch batch ({Status}): {Body}",
                    response.StatusCode, body);

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not reach the ERP API - will retry from the offline queue.");
                return false;
            }
        }
    }
}
