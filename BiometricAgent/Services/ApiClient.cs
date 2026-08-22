using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BiometricAgent.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BiometricAgent.Services
{
    /// <summary>
    /// All HTTP calls the agent makes to the central ERP API. Every method
    /// is best-effort: on any failure it logs (never the AgentKey/DeviceKey
    /// values themselves) and returns a failure result rather than throwing,
    /// so a single bad cycle never crashes the Worker's host process.
    /// </summary>
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
            _http.Timeout = TimeSpan.FromSeconds(30);
            _logger = logger;
        }

        // ------------------------------------------------------------
        // Register / Heartbeat / Device config
        // ------------------------------------------------------------

        /// <summary>POST /api/BiometricAgent/register - first contact on startup and periodically thereafter.</summary>
        public async Task<bool> RegisterAsync(CancellationToken ct)
        {
            var payload = new
            {
                AgentCode = _options.AgentCode,
                AgentKey = _options.AgentKey,
                MachineName = Environment.MachineName,
                AgentVersion = typeof(ApiClient).Assembly.GetName().Version?.ToString() ?? "1.0.0"
            };

            var response = await SendWithRetryAsync(
                () => BuildRequest(HttpMethod.Post, "api/BiometricAgent/register", payload),
                "register",
                ct);

            if (response == null)
                return false;

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Registered with the ERP API as agent {AgentCode}.", _options.AgentCode);
                return true;
            }

            await LogFailureAsync("register", response, ct);
            return false;
        }

        /// <summary>POST /api/BiometricAgent/heartbeat - lightweight liveness ping, independent of the device sync cycle.</summary>
        public async Task<bool> HeartbeatAsync(string? lastError, int queuedRecordCount, CancellationToken ct)
        {
            var payload = new
            {
                AgentCode = _options.AgentCode,
                AgentKey = _options.AgentKey,
                AgentVersion = typeof(ApiClient).Assembly.GetName().Version?.ToString() ?? "1.0.0",
                LastError = lastError,
                QueuedRecordCount = queuedRecordCount
            };

            var response = await SendWithRetryAsync(
                () => BuildRequest(HttpMethod.Post, "api/BiometricAgent/heartbeat", payload),
                "heartbeat",
                ct);

            if (response == null)
                return false;

            if (response.IsSuccessStatusCode)
                return true;

            await LogFailureAsync("heartbeat", response, ct);
            return false;
        }

        /// <summary>GET /api/BiometricAgent/devices - the active devices assigned to this agent.</summary>
        public async Task<List<AgentDeviceInfo>?> GetAssignedDevicesAsync(CancellationToken ct)
        {
            var response = await SendWithRetryAsync(
                () => BuildRequest(HttpMethod.Get, "api/BiometricAgent/devices"),
                "get-devices",
                ct);

            if (response == null)
                return null;

            if (!response.IsSuccessStatusCode)
            {
                await LogFailureAsync("get-devices", response, ct);
                return null;
            }

            try
            {
                return await response.Content.ReadFromJsonAsync<List<AgentDeviceInfo>>(cancellationToken: ct)
                       ?? new List<AgentDeviceInfo>();
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Could not parse the device list returned by the ERP API.");
                return null;
            }
        }

        // ------------------------------------------------------------
        // Test Connection (agent picks up + reports back)
        // ------------------------------------------------------------

        /// <summary>GET /api/BiometricAgent/pending-test-requests - polled every cycle alongside GetAssignedDevicesAsync.</summary>
        public async Task<List<AgentTestRequest>?> GetPendingTestRequestsAsync(CancellationToken ct)
        {
            var response = await SendWithRetryAsync(
                () => BuildRequest(HttpMethod.Get, "api/BiometricAgent/pending-test-requests"),
                "get-pending-test-requests",
                ct);

            if (response == null)
                return null;

            if (!response.IsSuccessStatusCode)
            {
                await LogFailureAsync("get-pending-test-requests", response, ct);
                return null;
            }

            try
            {
                return await response.Content.ReadFromJsonAsync<List<AgentTestRequest>>(cancellationToken: ct)
                       ?? new List<AgentTestRequest>();
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Could not parse the pending test request list returned by the ERP API.");
                return null;
            }
        }

        /// <summary>
        /// POST /api/BiometricAgent/test-result - reports the outcome of one
        /// Test Connection attempt. Only retried on transient failures like
        /// every other call here; if it never gets through, the API-side
        /// timeout in BiometricDeviceService.GetTestConnectionResultAsync
        /// eventually reports TimedOut to the user instead of leaving them
        /// waiting forever.
        /// </summary>
        public async Task<bool> SubmitTestResultAsync(AgentTestResultSubmission result, CancellationToken ct)
        {
            var response = await SendWithRetryAsync(
                () => BuildRequest(HttpMethod.Post, "api/BiometricAgent/test-result", result),
                $"test-result[{result.RequestId}]",
                ct);

            if (response == null)
                return false;

            if (response.IsSuccessStatusCode)
                return true;

            await LogFailureAsync($"test-result[{result.RequestId}]", response, ct);
            return false;
        }

        // ------------------------------------------------------------
        // Punch ingest
        // ------------------------------------------------------------

        /// <summary>
        /// Pushes a batch of punches for one device to POST /api/BiometricSync/ingest.
        /// Returns true only on a confirmed 2xx from the server - callers
        /// should keep the batch queued locally on any other outcome
        /// (network failure, 401 bad device key, 5xx, etc.).
        /// </summary>
        public async Task<bool> PushPunchesAsync(string deviceCode, string deviceKey, List<PunchRecord> punches, CancellationToken ct)
        {
            if (punches.Count == 0)
                return true;

            var payload = new
            {
                DeviceCode = deviceCode,
                DeviceKey = deviceKey,
                AgentCode = _options.AgentCode,
                Punches = punches.Select(p => new
                {
                    p.EmployeeCode,
                    p.PunchTime,
                    PunchType = (int)p.PunchType,
                    p.DeviceTransactionId
                })
            };

            var response = await SendWithRetryAsync(
                () => BuildRequest(HttpMethod.Post, "api/BiometricSync/ingest", payload),
                $"ingest[{deviceCode}]",
                ct);

            if (response == null)
                return false;

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Pushed {Count} punch(es) for device {DeviceCode} to the ERP API.", punches.Count, deviceCode);
                return true;
            }

            await LogFailureAsync($"ingest[{deviceCode}]", response, ct);
            return false;
        }

        // ------------------------------------------------------------
        // Retry / backoff plumbing
        // ------------------------------------------------------------

        private HttpRequestMessage BuildRequest(HttpMethod method, string path, object? payload = null)
        {
            var request = new HttpRequestMessage(method, path);

            if (payload != null)
                request.Content = JsonContent.Create(payload);

            request.Headers.Add("X-Tenant-ID", _options.TenantId);

            // GET endpoints (config, devices) carry the agent credential in
            // headers rather than a body; POST endpoints carry it in the
            // JSON payload (register/heartbeat/ingest) - matches how the API
            // controller reads each one.
            if (method == HttpMethod.Get)
            {
                request.Headers.Add("X-Agent-Code", _options.AgentCode);
                request.Headers.Add("X-Agent-Key", _options.AgentKey);
            }

            return request;
        }

        /// <summary>
        /// Sends one logical call with retry + exponential backoff for
        /// transient failures (network errors, timeouts, 5xx, 429). Permanent
        /// errors - 401 (bad key), 403 (forbidden), 404 (unknown
        /// route/resource) - are returned immediately without retrying, per
        /// the "never blindly retry invalid credentials" requirement.
        /// Returns null only when every attempt failed to even get a
        /// response (the caller should treat that as "unreachable").
        /// </summary>
        private async Task<HttpResponseMessage?> SendWithRetryAsync(
            Func<HttpRequestMessage> requestFactory,
            string operationName,
            CancellationToken ct)
        {
            var maxAttempts = Math.Max(1, _options.MaxRetryAttempts);

            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                ct.ThrowIfCancellationRequested();

                try
                {
                    using var request = requestFactory();
                    var response = await _http.SendAsync(request, ct);

                    if (IsPermanentFailure(response.StatusCode))
                        return response; // caller logs/handles - do not retry

                    if (response.IsSuccessStatusCode)
                        return response;

                    // Transient (5xx, 408, 429, or anything else unexpected).
                    if (attempt == maxAttempts)
                        return response;

                    _logger.LogWarning(
                        "{Operation} attempt {Attempt}/{Max} failed with {Status} - retrying.",
                        operationName, attempt, maxAttempts, response.StatusCode);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    throw; // graceful shutdown in progress - don't swallow
                }
                catch (Exception ex) when (attempt < maxAttempts)
                {
                    _logger.LogWarning(
                        ex, "{Operation} attempt {Attempt}/{Max} could not reach the ERP API - retrying.",
                        operationName, attempt, maxAttempts);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "{Operation} could not reach the ERP API after {Max} attempt(s).", operationName, maxAttempts);
                    return null;
                }

                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt - 1)); // 1s, 2s, 4s, ...
                await Task.Delay(delay, ct);
            }

            return null;
        }

        private static bool IsPermanentFailure(HttpStatusCode status) =>
            status is HttpStatusCode.Unauthorized
                or HttpStatusCode.Forbidden
                or HttpStatusCode.NotFound
                or HttpStatusCode.BadRequest;

        private async Task LogFailureAsync(string operationName, HttpResponseMessage response, CancellationToken ct)
        {
            string body;
            try
            {
                body = await response.Content.ReadAsStringAsync(ct);
            }
            catch
            {
                body = "<unreadable>";
            }

            var level = IsPermanentFailure(response.StatusCode) ? LogLevel.Error : LogLevel.Warning;

            // Never log AgentKey/DeviceKey - the response body from this API
            // never echoes them back, but stay defensive about future changes.
            _logger.Log(level,
                "{Operation} rejected by the ERP API ({Status}): {Body}",
                operationName, response.StatusCode, body);
        }
    }
}
