using System.Text.Json;
using BiometricAgent.Models;
using Microsoft.Extensions.Logging;

namespace BiometricAgent.Services
{
    /// <summary>
    /// Persists punches to disk when the ERP API can't be reached (client's
    /// internet link down, server maintenance, etc.), so nothing is lost -
    /// they get flushed on the next successful poll. Deliberately simple
    /// (one JSON file, whole-file rewrite) since punch volume per site is
    /// small (a few hundred/day at most).
    /// </summary>
    public class OfflineQueue
    {
        private readonly string _filePath;
        private readonly ILogger<OfflineQueue> _logger;
        private readonly SemaphoreSlim _lock = new(1, 1);

        public OfflineQueue(AgentOptions options, ILogger<OfflineQueue> logger)
        {
            Directory.CreateDirectory(options.StateFolder);
            _filePath = Path.Combine(options.StateFolder, "offline-queue.json");
            _logger = logger;
        }

        public async Task EnqueueAsync(List<PunchRecord> punches, CancellationToken ct)
        {
            if (punches.Count == 0)
                return;

            await _lock.WaitAsync(ct);
            try
            {
                var existing = await ReadAllAsync(ct);
                existing.AddRange(punches);
                await WriteAllAsync(existing, ct);
                _logger.LogInformation(
                    "Queued {Count} punch(es) locally (offline queue now has {Total}).",
                    punches.Count, existing.Count);
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task<List<PunchRecord>> PeekAllAsync(CancellationToken ct)
        {
            await _lock.WaitAsync(ct);
            try
            {
                return await ReadAllAsync(ct);
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task ClearAsync(CancellationToken ct)
        {
            await _lock.WaitAsync(ct);
            try
            {
                await WriteAllAsync(new List<PunchRecord>(), ct);
            }
            finally
            {
                _lock.Release();
            }
        }

        private async Task<List<PunchRecord>> ReadAllAsync(CancellationToken ct)
        {
            if (!File.Exists(_filePath))
                return new List<PunchRecord>();

            try
            {
                await using var stream = File.OpenRead(_filePath);
                var data = await JsonSerializer.DeserializeAsync<List<PunchRecord>>(stream, cancellationToken: ct);
                return data ?? new List<PunchRecord>();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Offline queue file was unreadable/corrupt - starting fresh.");
                return new List<PunchRecord>();
            }
        }

        private async Task WriteAllAsync(List<PunchRecord> punches, CancellationToken ct)
        {
            var tmpPath = _filePath + ".tmp";

            await using (var stream = File.Create(tmpPath))
            {
                await JsonSerializer.SerializeAsync(stream, punches, cancellationToken: ct);
            }

            File.Move(tmpPath, _filePath, overwrite: true);
        }
    }
}
