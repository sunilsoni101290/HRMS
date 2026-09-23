using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using Application.Interfaces.Attendances;

namespace Application.Services.Attendances
{
    /// <summary>
    /// Channel-backed implementation of IHistoricalAttendanceSyncJobQueue -
    /// same shape as EsslSyncJobQueue/AttendanceProcessingJobQueue, but a
    /// completely separate singleton instance/queue so a burst of
    /// Historical Sync activity can never interact with, delay, or be
    /// delayed by the eSSL queues.
    /// </summary>
    public class HistoricalAttendanceSyncJobQueue : IHistoricalAttendanceSyncJobQueue
    {
        private readonly Channel<HistoricalSyncJobRequest> _channel =
            Channel.CreateUnbounded<HistoricalSyncJobRequest>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            });

        public void Enqueue(HistoricalSyncJobRequest job)
        {
            _channel.Writer.TryWrite(job);
        }

        public IAsyncEnumerable<HistoricalSyncJobRequest> ReadAllAsync(CancellationToken ct)
            => _channel.Reader.ReadAllAsync(ct);
    }
}
