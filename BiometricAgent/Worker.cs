using BiometricAgent.Drivers;
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
            _logger.LogInformation(
                "BiometricAgent starting. Device {Code} at {Ip}:{Port}, pushing to {Api}, every {Interval}s.",
                _options.Device.DeviceCode, _options.Device.IPAddress, _options.Device.Port,
                _options.ApiBaseUrl, _options.PollIntervalSeconds);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await RunOneCycleAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // shutting down
                }
                catch (Exception ex)
                {
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
        }

        private async Task RunOneCycleAsync(CancellationToken ct)
        {
            // 1. Always try to flush anything queued from a previous failed push first,
            //    so punches are pushed in chronological order.
            var queued = await _offlineQueue.PeekAllAsync(ct);
            if (queued.Count > 0)
            {
                _logger.LogInformation("Retrying {Count} previously queued punch(es).", queued.Count);

                if (await _apiClient.PushPunchesAsync(queued, ct))
                {
                    await _offlineQueue.ClearAsync(ct);
                }
                else
                {
                    _logger.LogWarning("API still unreachable - skipping this cycle's device read to avoid growing the queue out of order.");
                    return;
                }
            }

            // 2. Pull new punches from the device.
            using var driver = DeviceDriverFactory.Create(_options.Device, _loggerFactory);

            var connected = await driver.ConnectAsync(ct);
            if (!connected)
            {
                _logger.LogWarning("Skipping this cycle - could not connect to the biometric device.");
                return;
            }

            var since = _syncState.GetLastSyncedTime();
            List<Models.PunchRecord> punches;

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
                _logger.LogDebug("No new punches since {Since}.", since);
                return;
            }

            _logger.LogInformation("Read {Count} new punch(es) from the device.", punches.Count);

            // 3. Push to the central API; queue locally on any failure.
            if (await _apiClient.PushPunchesAsync(punches, ct))
            {
                var latest = punches.Max(p => p.PunchTime);
                _syncState.SetLastSyncedTime(latest);
            }
            else
            {
                await _offlineQueue.EnqueueAsync(punches, ct);
            }
        }
    }
}
