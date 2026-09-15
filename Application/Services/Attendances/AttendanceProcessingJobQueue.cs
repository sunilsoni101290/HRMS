using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using Application.Interfaces.Attendances;

namespace Application.Services.Attendances
{
    /// <summary>
    /// Channel-backed implementation of IAttendanceProcessingJobQueue - see
    /// that interface's remarks. Registered as a singleton so every
    /// controller (producer) and EsslAttendanceSyncBackgroundService's new
    /// consumer loop share one instance for the app's lifetime, exactly like
    /// EsslSyncJobQueue.
    ///
    /// Coalescing: a full processor run already drains EVERY unprocessed
    /// row regardless of which request triggered it, so queuing more than
    /// one "please drain" request at a time is pure waste - a burst of
    /// BiometricSyncController.Ingest calls (an on-site agent can push every
    /// few seconds) would otherwise queue dozens of near-empty runs back to
    /// back. _pending is flipped back to 0 as soon as ReadAllAsync yields a
    /// job to the consumer (i.e. right BEFORE that job's processing starts,
    /// not after it finishes) - so a request that arrives while a drain is
    /// already running is never lost, it simply enqueues the NEXT run
    /// instead of being silently absorbed into the one already in flight
    /// (which may already have read its batch before that request's rows
    /// were even inserted).
    /// </summary>
    public class AttendanceProcessingJobQueue : IAttendanceProcessingJobQueue
    {
        private readonly Channel<AttendanceProcessingJobRequest> _channel =
            Channel.CreateUnbounded<AttendanceProcessingJobRequest>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            });

        private int _pending;

        public void Enqueue(AttendanceProcessingJobRequest job)
        {
            // Only one request needs to be in flight at a time (see class
            // remarks) - if one is already queued/running, this call is a
            // harmless no-op; the eventual run will still see every row
            // inserted up to the moment it actually queries the backlog.
            if (Interlocked.Exchange(ref _pending, 1) == 1)
                return;

            _channel.Writer.TryWrite(job);
        }

        public async IAsyncEnumerable<AttendanceProcessingJobRequest> ReadAllAsync(
            [EnumeratorCancellation] CancellationToken ct)
        {
            await foreach (var job in _channel.Reader.ReadAllAsync(ct))
            {
                // Reset right as this job is handed to the consumer (not
                // after it finishes) - see class remarks.
                Interlocked.Exchange(ref _pending, 0);
                yield return job;
            }
        }
    }
}
