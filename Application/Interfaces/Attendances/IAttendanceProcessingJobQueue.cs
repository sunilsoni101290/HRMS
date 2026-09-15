using System.Collections.Generic;
using System.Threading;

namespace Application.Interfaces.Attendances
{
    /// <summary>
    /// One queued "drain the biometric attendance backlog" request - the
    /// LAN-agent side of the pipeline (BiometricSyncController's
    /// Ingest/Sync/SyncAll actions) used to call
    /// IAttendanceProcessorService.ProcessAttendanceAsync() directly,
    /// in-line, before returning the HTTP response. That was fine when a
    /// batch import was small, but AttendanceProcessorService now drains the
    /// ENTIRE unprocessed backlog (up to MaxBatchesPerRun * batchSize rows,
    /// not just whatever this request just imported) every time it runs -
    /// so an agent's routine "push a handful of new punches" call could end
    /// up blocking on tens of thousands of unrelated backlog rows. This
    /// queue lets those endpoints enqueue the drain and return immediately
    /// (requirement: "API should enqueue the job and return quickly"),
    /// exactly the same fix already applied to the eSSL DB-pull path via
    /// IEsslSyncJobQueue - same Channel-backed pattern, same consuming
    /// BackgroundService (EsslAttendanceSyncBackgroundService gets a second,
    /// independent loop for this queue rather than a new hosted service).
    /// </summary>
    public class AttendanceProcessingJobRequest
    {
        /// <summary>Correlates this queued request to logs - not a persisted, resumable job id.</summary>
        public string JobId { get; set; } = "";

        /// <summary>Which endpoint/action enqueued this, for logging only (e.g. "BiometricSyncController.Ingest").</summary>
        public string TriggeredBy { get; set; } = "";
    }

    /// <summary>
    /// In-process background work queue for "please run
    /// AttendanceProcessorService now" requests. Deliberately in-memory only
    /// (System.Threading.Channels) - same accepted trade-off as
    /// IEsslSyncJobQueue: a request still sitting in this queue is lost on
    /// an app restart before it's picked up, but that only means the drain
    /// happens on the next agent push or the next periodic eSSL sync cycle
    /// instead (both of which already trigger the same processor), never a
    /// correctness issue - AttendanceProcessorService's own IsProcessed flag
    /// is what actually protects against double-processing across a
    /// restart, not this queue.
    /// </summary>
    public interface IAttendanceProcessingJobQueue
    {
        /// <summary>Enqueues a "drain the backlog" request and returns immediately - never blocks on the processing itself.</summary>
        void Enqueue(AttendanceProcessingJobRequest job);

        /// <summary>Streams queued jobs as they arrive - the consumer loop's only read path.</summary>
        IAsyncEnumerable<AttendanceProcessingJobRequest> ReadAllAsync(CancellationToken ct);
    }
}
