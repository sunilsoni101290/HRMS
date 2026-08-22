using BiometricAgent.Drivers;
using BiometricAgent.Models;
using BiometricAgent.Services;
using Microsoft.Extensions.Options;

namespace BiometricAgent
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly AgentOptions _options;
        private readonly ApiClient _apiClient;
        private readonly OfflineQueue _offlineQueue;
        private readonly SyncState _syncState;

        private bool _registered;
        private DateTime _lastHeartbeat = DateTime.MinValue;
        private List<AgentDeviceInfo> _knownDevices = new();

        public Worker(
            ILogger<Worker> logger,
            ILoggerFactory loggerFactory,
            IOptions<AgentOptions> options,
            ApiClient apiClient,
            OfflineQueue offlineQueue,
            SyncState syncState)
        {
            _logger = logger;
            _loggerFactory = loggerFactory;
            _options = options.Value;
            _apiClient = apiClient;
            _offlineQueue = offlineQueue;
            _syncState = syncState;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (string.IsNullOrWhiteSpace(_options.AgentCode) || string.IsNullOrWhiteSpace(_options.AgentKey))
            {
                _logger.LogCritical(
                    "Agent:AgentCode / Agent:AgentKey are not configured. Create the BiometricAgent record " +
                    "from the ERP MVC first, then copy its AgentCode/AgentKey into this service's configuration. " +
                    "The service will keep retrying registration but cannot succeed until this is fixed.");
            }

            _logger.LogInformation(
                "BiometricAgent starting. AgentCode {AgentCode}, API {Api}, poll every {Interval}s.",
                _options.AgentCode, _options.ApiBaseUrl, _options.PollIntervalSeconds);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await RunOneCycleAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    // graceful shutdown - fall through to the outer loop's exit check
                }
                catch (Exception ex)
                {
                    // Nothing thrown from a single cycle should ever bring
                    // down the Windows Service process - log and retry next interval.
                    _logger.LogError(ex, "Unexpected error in sync cycle - will retry next interval.");
                }

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(_options.PollIntervalSeconds), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // shutting down
                }
            }

            _logger.LogInformation("BiometricAgent stop requested - exiting cleanly.");
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("BiometricAgent stopping (graceful shutdown)...");
            await base.StopAsync(cancellationToken);
            _logger.LogInformation("BiometricAgent stopped.");
        }

        private async Task RunOneCycleAsync(CancellationToken ct)
        {
            // 1. Register (first cycle only, or after a previous failure) -
            //    every subsequent action needs the API to actually know this agent.
            if (!_registered)
            {
                _registered = await _apiClient.RegisterAsync(ct);

                if (!_registered)
                {
                    _logger.LogWarning("Registration failed - will retry next cycle. Skipping device sync for now.");
                    return;
                }
            }

            // 2. Heartbeat - independent liveness signal, sent on its own
            //    interval so it isn't skipped just because a device read is slow.
            if ((DateTime.UtcNow - _lastHeartbeat).TotalSeconds >= _options.HeartbeatIntervalSeconds)
            {
                var queuedTotal = await CountAllQueuedAsync(ct);
                var heartbeatOk = await _apiClient.HeartbeatAsync(lastError: null, queuedRecordCount: queuedTotal, ct);

                if (heartbeatOk)
                {
                    _lastHeartbeat = DateTime.UtcNow;
                }
                else
                {
                    // A rejected heartbeat (e.g. agent disabled by an admin,
                    // or key rotated) means we should re-register before
                    // trusting anything else this cycle.
                    _logger.LogWarning("Heartbeat rejected - will re-register next cycle.");
                    _registered = false;
                    return;
                }
            }

            // 3. Process any pending Test Connection requests FIRST, before
            //    the regular device sync below - a user waiting on the
            //    "Test Connection" button shouldn't sit behind a slow punch
            //    sync cycle. Independent of device sync succeeding/failing;
            //    one never blocks the other.
            await ProcessPendingTestRequestsAsync(ct);

            // 4. Refresh the assigned device list from the API. On failure,
            //    keep using the last known list so a transient API blip
            //    doesn't stop already-configured devices from being polled.
            var devices = await _apiClient.GetAssignedDevicesAsync(ct);

            if (devices != null)
            {
                _knownDevices = devices.Where(d => d.IsActive).ToList();
            }
            else if (_knownDevices.Count == 0)
            {
                _logger.LogWarning("Could not retrieve the assigned device list and no cached list exists yet - nothing to sync this cycle.");
                return;
            }
            else
            {
                _logger.LogWarning("Could not refresh the assigned device list - continuing with the last known {Count} device(s).", _knownDevices.Count);
            }

            if (_knownDevices.Count == 0)
            {
                _logger.LogDebug("No active devices are currently assigned to this agent.");
                return;
            }

            // 5. Process every assigned device independently - one device's
            //    failure (unreachable, driver error, etc.) must never stop
            //    the others from being polled.
            foreach (var device in _knownDevices)
            {
                ct.ThrowIfCancellationRequested();

                try
                {
                    await ProcessDeviceAsync(device, ct);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unhandled error processing device {DeviceCode} - continuing with remaining devices.", device.DeviceCode);
                }
            }
        }

        /// <summary>
        /// Executes every Pending Test Connection request routed to this
        /// agent: Connect -> (best-effort) probe -> Disconnect, exactly the
        /// same driver lifecycle as ProcessDeviceAsync's punch read, but
        /// deliberately NOT sharing a connection with it - a Test Connection
        /// is a standalone, non-destructive round trip that must never read/
        /// delete attendance logs or touch device configuration. One
        /// request's failure never stops the others from being attempted.
        /// </summary>
        private async Task ProcessPendingTestRequestsAsync(CancellationToken ct)
        {
            var requests = await _apiClient.GetPendingTestRequestsAsync(ct);

            if (requests == null || requests.Count == 0)
                return;

            _logger.LogInformation("{Count} pending test connection request(s) to process.", requests.Count);

            foreach (var request in requests)
            {
                ct.ThrowIfCancellationRequested();

                try
                {
                    await ProcessTestRequestAsync(request, ct);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    // Defense in depth - ProcessTestRequestAsync already
                    // catches driver exceptions itself and reports a safe
                    // failure result, so reaching here means something
                    // unexpected (e.g. the result POST itself threw). Log
                    // only; never let one bad request stop the others or
                    // crash the cycle.
                    _logger.LogError(ex, "Unhandled error processing test connection request {RequestId} for device {DeviceCode} - continuing with remaining requests.",
                        request.RequestId, request.DeviceCode);
                }
            }
        }

        private async Task ProcessTestRequestAsync(AgentTestRequest request, CancellationToken ct)
        {
            _logger.LogInformation(
                "Agent attempting ESSL connection. DeviceCode: {DeviceCode}, DeviceType: {DeviceType}, IP: {Ip}, Port: {Port}, RequestId: {RequestId}",
                request.DeviceCode, request.DeviceType, request.IPAddress, request.Port, request.RequestId);

            var driverOptions = new DeviceOptions
            {
                Id = request.DeviceId,
                DriverType = request.DeviceType,
                DeviceCode = request.DeviceCode,
                DeviceKey = request.DeviceKey,
                IPAddress = request.IPAddress,
                Port = request.Port,
                CommKey = request.CommKey
            };

            AgentTestResultSubmission result;

            try
            {
                using var driver = DeviceDriverFactory.Create(driverOptions, _loggerFactory);

                var connected = await driver.ConnectAsync(ct);

                if (!connected)
                {
                    // Connect_Net returned false with no exception - the SDK
                    // handshake itself was rejected/timed out. Worth calling
                    // out separately from a raw socket failure (caught
                    // below) since this app's known-good network case
                    // (TCP reachable, SDK still fails) points at the device/
                    // firmware/comm-password layer, not the wire.
                    _logger.LogWarning(
                        "Test connection failed. DeviceCode: {DeviceCode}, IP: {Ip}, Port: {Port}, Stage: SDK Connection",
                        request.DeviceCode, request.IPAddress, request.Port);

                    result = BuildResult(request, success: false, stage: "SdkConnectionFailed",
                        message: "The ESSL device could not be initialized or did not respond.");
                }
                else
                {
                    _logger.LogInformation("ESSL connection successful. DeviceCode: {DeviceCode}", request.DeviceCode);

                    string? deviceInfo = null;

                    try
                    {
                        deviceInfo = await driver.TryGetDeviceInfoAsync(ct);
                    }
                    finally
                    {
                        await driver.DisconnectAsync();
                    }

                    _logger.LogInformation("Device communication verified. DeviceCode: {DeviceCode}. Test connection successful.", request.DeviceCode);

                    result = BuildResult(request, success: true, stage: "Success",
                        message: $"Connection successful. ESSL device {request.DeviceCode} is responding.", deviceInfo: deviceInfo);
                }
            }
            catch (System.Net.Sockets.SocketException ex)
            {
                // A real wire-level failure (connection refused/timed out/
                // host unreachable) - distinct from the device/SDK rejecting
                // a technically-established connection.
                _logger.LogError(ex,
                    "Test connection failed. DeviceCode: {DeviceCode}, IP: {Ip}, Port: {Port}, Stage: TCP Connection, RequestId: {RequestId}",
                    request.DeviceCode, request.IPAddress, request.Port, request.RequestId);

                result = BuildResult(request, success: false, stage: "DeviceUnreachable",
                    message: "The biometric agent could not reach the biometric device.");
            }
            catch (Exception ex)
            {
                // Never forward ex.Message/ex.ToString() verbatim - keep the
                // stage-specific safe message, log the real exception here
                // (agent-side log, never sent to the API/UI).
                _logger.LogError(ex,
                    "Test connection failed. DeviceCode: {DeviceCode}, IP: {Ip}, Port: {Port}, Stage: SDK Connection, RequestId: {RequestId}",
                    request.DeviceCode, request.IPAddress, request.Port, request.RequestId);

                result = BuildResult(request, success: false, stage: "SdkConnectionFailed",
                    message: "The ESSL device could not be initialized or did not respond.");
            }

            await _apiClient.SubmitTestResultAsync(result, ct);
        }

        private AgentTestResultSubmission BuildResult(AgentTestRequest request, bool success, string stage, string message, string? deviceInfo = null) => new()
        {
            RequestId = request.RequestId,
            AgentCode = _options.AgentCode,
            AgentKey = _options.AgentKey,
            Success = success,
            Stage = stage,
            Message = message,
            DeviceInfo = deviceInfo
        };

        private async Task ProcessDeviceAsync(AgentDeviceInfo device, CancellationToken ct)
        {
            // 4a. Always try to flush anything queued from a previous failed
            //     push first, so punches are pushed in chronological order.
            var queued = await _offlineQueue.PeekAllAsync(device.DeviceCode, ct);
            if (queued.Count > 0)
            {
                _logger.LogInformation("Retrying {Count} previously queued punch(es) for device {DeviceCode}.", queued.Count, device.DeviceCode);

                if (await _apiClient.PushPunchesAsync(device.DeviceCode, device.DeviceKey, queued, ct))
                {
                    await _offlineQueue.ClearAsync(device.DeviceCode, ct);
                }
                else
                {
                    _logger.LogWarning(
                        "API still unreachable for device {DeviceCode} - skipping this cycle's device read to avoid growing the queue out of order.",
                        device.DeviceCode);
                    return;
                }
            }

            // 4b. Pull new punches from the physical device.
            var driverOptions = new DeviceOptions
            {
                Id = device.Id,
                DriverType = device.DeviceType,
                DeviceCode = device.DeviceCode,
                DeviceKey = device.DeviceKey,
                IPAddress = device.IPAddress,
                Port = device.Port,
                // From the ERP-assigned device config - NEVER hard-coded
                // here. See AgentDeviceInfo/BiometricDeviceService.GetByAgentAsync.
                CommKey = device.CommKey
            };

            using var driver = DeviceDriverFactory.Create(driverOptions, _loggerFactory);

            var connected = await driver.ConnectAsync(ct);
            if (!connected)
            {
                _logger.LogWarning("Skipping device {DeviceCode} this cycle - could not connect.", device.DeviceCode);
                return;
            }

            var since = _syncState.GetLastSyncedTime(device.DeviceCode);
            List<PunchRecord> punches;

            try
            {
                punches = await driver.GetNewPunchesAsync(since, ct);
            }
            finally
            {
                await driver.DisconnectAsync();
            }

            if (punches.Count == 0)
            {
                _logger.LogDebug("No new punches for device {DeviceCode} since {Since}.", device.DeviceCode, since);
                return;
            }

            _logger.LogInformation("Read {Count} new punch(es) from device {DeviceCode}.", punches.Count, device.DeviceCode);

            // 4c. Push to the central API; queue locally on any failure so
            //     nothing collected from the device is ever silently dropped.
            if (await _apiClient.PushPunchesAsync(device.DeviceCode, device.DeviceKey, punches, ct))
            {
                var latest = punches.Max(p => p.PunchTime);
                _syncState.SetLastSyncedTime(device.DeviceCode, latest);
            }
            else
            {
                await _offlineQueue.EnqueueAsync(device.DeviceCode, punches, ct);
            }
        }

        private async Task<int> CountAllQueuedAsync(CancellationToken ct)
        {
            var total = 0;

            foreach (var device in _knownDevices)
            {
                total += (await _offlineQueue.PeekAllAsync(device.DeviceCode, ct)).Count;
            }

            return total;
        }
    }
}
