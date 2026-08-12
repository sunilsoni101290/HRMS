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

            // 3. Refresh the assigned device list from the API. On failure,
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

            // 4. Process every assigned device independently - one device's
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
                Port = device.Port
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
