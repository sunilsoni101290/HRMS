using System.Collections.Generic;
using System.Threading;
using Application.DTOs.Attendances;

namespace Application.Interfaces.Attendances
{
    /// <summary>
    /// One queued manual eSSL sync request (Sync Now / Historical Import /
    /// Retry Failed Sync) - everything EsslAttendanceSyncBackgroundService's
    /// queue-consumer loop needs to replay the exact same
    /// IEsslAttendanceSyncService.SyncAsync call the API controller used to
    /// make directly.
    /// </summary>
    public class EsslSyncJobRequest
    {
        /// <summary>
        /// Correlates this queued request to what the UI shows while
        /// waiting/polling - not a persisted, resumable job id (see
        /// IEsslSyncJobQueue's remarks on what does and doesn't survive an
        /// app restart). Generated once when the job is enqueued.
        /// </summary>
        public string JobId { get; set; } = "";

        public string TenantId { get; set; } = "";

        public EsslSyncRequestDto Request { get; set; } = new();

        public string TriggeredBy { get; set; } = "";
    }

    /// <summary>
    /// In-process background work queue for manual eSSL sync requests
    /// (Sync Now / Historical Import / Retry Failed Sync), consumed by
    /// EsslAttendanceSyncBackgroundService - the SAME already-registered,
    /// framework-managed BackgroundService that already runs the automatic
    /// periodic sync, rather than a one-off detached Task.Run per request.
    /// A request queued here is handed to the exact same
    /// IEsslAttendanceSyncService.SyncAsync core engine either way -
    /// nothing about the sync logic itself changes, only WHERE it runs.
    ///
    /// Deliberately in-memory only (System.Threading.Channels), not a
    /// database-backed durable job table: a request still sitting in this
    /// queue (not yet started) is lost if the app restarts before the host
    /// picks it up. This is an accepted, explicitly-scoped trade-off, not
    /// an oversight - see EsslAttendanceSyncBackgroundService's remarks for
    /// why it's safe: nothing partially-written or corrupted results (the
    /// sync engine's own per-batch checkpoint/duplicate-prevention design
    /// is what actually protects correctness across a restart, and that is
    /// unaffected), the worst case is simply that the admin needs to click
    /// the button again after the app comes back up. A fully durable,
    /// auto-resuming job queue (a persisted table + this same consumer
    /// loop reading from it instead) is a reasonable future enhancement if
    /// that guarantee is ever needed, but is out of scope here.
    /// </summary>
    public interface IEsslSyncJobQueue
    {
        /// <summary>Enqueues a job and returns immediately - never blocks on the sync itself.</summary>
        void Enqueue(EsslSyncJobRequest job);

        /// <summary>Streams queued jobs as they arrive - the consumer loop's only read path.</summary>
        IAsyncEnumerable<EsslSyncJobRequest> ReadAllAsync(CancellationToken ct);
    }
}
