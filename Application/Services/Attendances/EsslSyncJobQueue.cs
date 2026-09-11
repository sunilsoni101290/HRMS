using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using Application.Interfaces.Attendances;

namespace Application.Services.Attendances
{
    /// <summary>
    /// Channel-backed implementation of IEsslSyncJobQueue - the standard
    /// .NET "background task queue" pattern (System.Threading.Channels),
    /// registered as a singleton so the API controller (producer) and
    /// EsslAttendanceSyncBackgroundService (consumer) share the same queue
    /// instance for the lifetime of the app. Unbounded: a burst of manual
    /// sync clicks (unlikely in practice - the API controller only enqueues
    /// when GetSettingsAsync just reported the tenant as not already
    /// running) can never make Enqueue itself block or fail.
    /// </summary>
    public class EsslSyncJobQueue : IEsslSyncJobQueue
    {
        private readonly Channel<EsslSyncJobRequest> _channel =
            Channel.CreateUnbounded<EsslSyncJobRequest>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            });

        public void Enqueue(EsslSyncJobRequest job)
        {
            // TryWrite always succeeds on an unbounded channel (never
            // awaits, never blocks the calling HTTP request).
            _channel.Writer.TryWrite(job);
        }

        public IAsyncEnumerable<EsslSyncJobRequest> ReadAllAsync(CancellationToken ct)
            => _channel.Reader.ReadAllAsync(ct);
    }
}
