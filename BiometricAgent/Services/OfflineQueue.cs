using System.Collections.Concurrent;
using System.Text.Json;
using BiometricAgent.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BiometricAgent.Services
{
    /// <summary>
    /// Persists punches to disk when the ERP API can't be reached (client's
    /// internet link down, server maintenance, etc.), so nothing is lost -
    /// they get flushed on the next successful poll. One queue file per
    /// device (keyed by DeviceCode) so devices retry independently - one
    /// device stuck offline never blocks another device's queue from draining.
    /// Deliberately simple (whole-file rewrite per device) since punch volume
    /// per site is small (a few hundred/day at most per device).
    /// </summary>
    public class OfflineQueue
    {
        private readonly string _folder;
        private readonly ILogger<OfflineQueue> _logger;

        // One semaphore per device file, created on first use, so concurrent
        // access to different devices' queues never blocks on each other.
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

        public OfflineQueue(IOptions<AgentOptions> options, ILogger<OfflineQueue> logger)
        {
            _folder = options.Value.StateFolder;
            Directory.CreateDirectory(_folder);
            _logger = logger;
        }

        public async Task EnqueueAsync(string deviceCode, List<PunchRecord> punches, CancellationToken ct)
        {
            if (punches.Count == 0)
                return;

            var gate = GateFor(deviceCode);
            await gate.WaitAsync(ct);
            try
            {
                var existing = await ReadAllAsync(deviceCode, ct);
                existing.AddRange(punches);
                await WriteAllAsync(deviceCode, existing, ct);
                _logger.LogInformation(
                    "Queued {Count} punch(es) locally for device {DeviceCode} (offline queue now has {Total}).",
                    punches.Count, deviceCode, existing.Count);
            }
            finally
            {
                gate.Release();
            }
        }

        public async Task<List<PunchRecord>> PeekAllAsync(string deviceCode, CancellationToken ct)
        {
            var gate = GateFor(deviceCode);
            await gate.WaitAsync(ct);
            try
            {
                return await ReadAllAsync(deviceCode, ct);
            }
            finally
            {
                gate.Release();
            }
        }

        public async Task ClearAsync(string deviceCode, CancellationToken ct)
        {
            var gate = GateFor(deviceCode);
            await gate.WaitAsync(ct);
            try
            {
                await WriteAllAsync(deviceCode, new List<PunchRecord>(), ct);
            }
            finally
            {
                gate.Release();
            }
        }

        private SemaphoreSlim GateFor(string deviceCode) =>
            _locks.GetOrAdd(deviceCode, _ => new SemaphoreSlim(1, 1));

        private string FilePath(string deviceCode) =>
            Path.Combine(_folder, $"offline-queue-{SanitizeForFileName(deviceCode)}.json");

        private static string SanitizeForFileName(string value)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
                value = value.Replace(c, '_');
            return value;
        }

        private async Task<List<PunchRecord>> ReadAllAsync(string deviceCode, CancellationToken ct)
        {
            var path = FilePath(deviceCode);

            if (!File.Exists(path))
                return new List<PunchRecord>();

            try
            {
                await using var stream = File.OpenRead(path);
                var data = await JsonSerializer.DeserializeAsync<List<PunchRecord>>(stream, cancellationToken: ct);
                return data ?? new List<PunchRecord>();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Offline queue file for device {DeviceCode} was unreadable/corrupt - starting fresh.", deviceCode);
                return new List<PunchRecord>();
            }
        }

        private async Task WriteAllAsync(string deviceCode, List<PunchRecord> punches, CancellationToken ct)
        {
            var path = FilePath(deviceCode);
            var tmpPath = path + ".tmp";

            await using (var stream = File.Create(tmpPath))
            {
                await JsonSerializer.SerializeAsync(stream, punches, cancellationToken: ct);
            }

            File.Move(tmpPath, path, overwrite: true);
        }
    }
}
