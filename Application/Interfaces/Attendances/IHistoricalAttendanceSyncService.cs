using Application.DTOs.Attendances;
using Application.DTOs.Employee;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Interfaces.Attendances
{
    /// <summary>
    /// Standalone Historical Attendance Sync engine - reads ONLY existing
    /// dbo.BiometricAttendanceLogs rows for an admin-picked historical
    /// From/To window and turns them into AttendanceLogs/Attendances via
    /// dbo.ProcessHistoricalBiometricAttendance. Never fetches from eSSL,
    /// never touches EsslAttendanceSyncState/IEsslAttendanceSyncService, and
    /// is never called by the eSSL sync pipeline - see the "out of scope"
    /// list in the feature spec / this interface's siblings
    /// (IEsslAttendanceSyncService.cs, IEsslSyncJobQueue.cs) which remain
    /// completely unmodified.
    /// </summary>
    public interface IHistoricalAttendanceSyncService
    {
        /// <summary>Read-only - counts what a run over this window would touch, without writing anything.</summary>
        Task<HistoricalSyncPreviewDto> PreviewAsync(HistoricalSyncRequestDto request, string tenantId);

        /// <summary>
        /// Atomically checks for an overlapping Queued/Running job for this
        /// tenant AND, if none, creates the new HistoricalAttendanceSyncJob
        /// row (Status = Queued) in the same call - closes the same
        /// enqueue-then-poll race window ClaimSyncLockAsync closes for the
        /// eSSL pipeline (see API's EsslAttendanceController.SyncNow
        /// remarks), using this feature's OWN, separate lock
        /// (HistoricalAttendanceSyncJob.IsRunning + overlapping-range check)
        /// - never EsslAttendanceSyncState.
        /// </summary>
        Task<(bool Claimed, string? Message, string? JobId)> ClaimAndCreateJobAsync(
            HistoricalSyncRequestDto request, string tenantId, string requestedBy);

        /// <summary>
        /// Runs one already-created (Status = Queued) job to completion -
        /// called by HistoricalAttendanceSyncBackgroundService's consumer
        /// loop, never inline from the API controller (spec section 6:
        /// "Start... must enqueue... and return immediately").
        /// </summary>
        Task RunJobAsync(string jobId, CancellationToken ct);

        Task<HistoricalSyncJobDto?> GetJobStatusAsync(string jobId, string tenantId);

        Task<PagedResult<HistoricalSyncJobDto>> GetJobHistoryAsync(HistoricalSyncJobFilterDto filter, string tenantId);

        /// <summary>Startup-only reconciliation of any job left IsRunning=true by a previous process instance - see HistoricalAttendanceSyncJob.IsRunning's remarks. Never called from user-facing code.</summary>
        Task ReconcileStaleLocksAsync();
    }
}
