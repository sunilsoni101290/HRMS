using System.Collections.Generic;
using System.Threading;

namespace Application.Interfaces.Attendances
{
    /// <summary>
    /// One queued "please run this already-created Historical Attendance
    /// Sync job now" request. Its own, separate in-process queue - NOT
    /// IEsslSyncJobQueue (which stays untouched, still only ever carries
    /// eSSL Sync Now / Historical Import[eSSL wording]/ Retry requests) -
    /// consumed by the NEW HistoricalAttendanceSyncBackgroundService, never
    /// by EsslAttendanceSyncBackgroundService. Same Channel-backed,
    /// in-memory-only trade-off as IEsslSyncJobQueue/
    /// IAttendanceProcessingJobQueue: a request still sitting in this queue
    /// is lost on an app restart before it's picked up, but the job row
    /// itself (Status = Queued) survives in the database - see
    /// HistoricalAttendanceSyncBackgroundService's startup sweep, which
    /// re-enqueues any job still Queued so a restart never silently drops
    /// it forever.
    /// </summary>
    public class HistoricalSyncJobRequest
    {
        public string JobId { get; set; } = "";
        public string TenantId { get; set; } = "";
        public string TriggeredBy { get; set; } = "";
    }

    public interface IHistoricalAttendanceSyncJobQueue
    {
        void Enqueue(HistoricalSyncJobRequest job);

        IAsyncEnumerable<HistoricalSyncJobRequest> ReadAllAsync(CancellationToken ct);
    }
}
