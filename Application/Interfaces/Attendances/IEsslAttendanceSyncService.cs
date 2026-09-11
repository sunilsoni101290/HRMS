using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Application.DTOs.Attendances;
using Application.DTOs.Employee;

namespace Application.Interfaces.Attendances
{
    /// <summary>
    /// The one core sync engine for the eSSL eTimeTrackLite1 direct-SQL
    /// integration - used by BOTH the automatic background service and the
    /// manual/historical "Sync Now" admin action (requirement: "Manual
    /// synchronization must use the same core service as automatic
    /// synchronization. Do not duplicate synchronization logic.").
    /// </summary>
    public interface IEsslAttendanceSyncService
    {
        /// <summary>
        /// Runs one sync pass. request.FromDate/ToDate both null = automatic
        /// incremental mode (uses the persisted EsslAttendanceSyncState
        /// cursor). Either set = manual/historical mode over that explicit
        /// window. Safe to call concurrently - internally serialized per
        /// tenant via EsslAttendanceSyncState.IsSyncRunning (requirement
        /// #12's "appropriate locking").
        /// </summary>
        /// <summary>
        /// lockAlreadyClaimed: pass true only when the caller has already
        /// atomically claimed this tenant's sync lock itself via
        /// ClaimSyncLockAsync (this is what the manual "Sync Now"/
        /// "Historical Import"/"Retry Failed Sync" path does now, BEFORE
        /// enqueueing the job - see EsslAttendanceController.SyncNow's
        /// remarks) - in that case SyncAsync skips its own claim/stale-
        /// check step entirely (claiming it a second time would otherwise
        /// make SyncAsync see its own just-claimed lock and incorrectly
        /// refuse to run). The automatic background cycle never pre-claims
        /// and always leaves this false/default, so SyncAsync claims the
        /// lock itself exactly as before for that path.
        /// </summary>
        Task<EsslSyncResultDto> SyncAsync(
            EsslSyncRequestDto request,
            string tenantId,
            string triggeredBy,
            CancellationToken ct = default,
            bool lockAlreadyClaimed = false);

        /// <summary>
        /// Atomically claims this tenant's sync lock (same stale-lock
        /// take-over rule SyncAsync's own internal claim uses) WITHOUT
        /// running a sync - lets a caller (the SyncNow controller action)
        /// guarantee the lock, and the EsslAttendanceSyncState row itself,
        /// are already committed to the database before it responds to the
        /// browser. This closes a real race: without it, a client that
        /// starts polling GetStatus immediately after "Queued" comes back
        /// could poll before the background consumer had even picked the
        /// job off the queue, see IsSyncRunning still false (or, for a
        /// tenant with no EsslAttendanceSyncState row yet, no row at all),
        /// and wrongly conclude the sync had already finished - showing a
        /// "Sync Complete" popup with blank/undefined counters while the
        /// real sync was still running in the background.
        /// </summary>
        Task<(bool Success, string Message)> ClaimSyncLockAsync(string tenantId);

        /// <summary>Card 1 "Integration Status" - runtime/status fields only, never configuration.</summary>
        Task<EsslSyncSettingsDto> GetSettingsAsync(string tenantId);

        /// <summary>Card 2 "Database Configuration" GET - never includes the password, only HasPasswordConfigured.</summary>
        Task<EsslDatabaseConfigViewDto> GetConfigurationAsync(string tenantId);

        /// <summary>
        /// Validates and persists the Database Configuration form. Blank/null
        /// dto.Password keeps the existing saved password unchanged. Does
        /// NOT test the connection itself and does NOT start a sync - the
        /// UI is expected to call TestConnectionAsync separately if desired.
        /// </summary>
        Task<(bool Success, string Message)> SaveConfigurationAsync(EsslDatabaseConfigDto dto, string tenantId, string modifiedBy);

        /// <summary>
        /// Tests the exact values currently in the form (requirement #6) -
        /// never the saved configuration. Blank/null dto.Password falls back
        /// to the tenant's already-saved password (if any), so a user
        /// testing connectivity right after opening the page (without
        /// retyping a password they already saved) still gets a meaningful
        /// result. Never saves anything, never starts a sync.
        /// </summary>
        Task<(bool Success, string Message)> TestConnectionAsync(EsslDatabaseConfigDto dto, string tenantId);

        Task<PagedResult<EsslSyncHistoryDto>> GetSyncHistoryAsync(EsslSyncHistoryFilterDto filter, string tenantId);

        Task<List<EsslUnmappedEmployeeDto>> GetUnmappedEmployeesAsync(string tenantId);

        /// <summary>
        /// Safe manual recovery for a tenant whose IsSyncRunning lock is
        /// genuinely stuck (e.g. the process that held it crashed or was
        /// killed, or an app restart happened mid-sync and, for some
        /// reason, the automatic startup reconciliation in
        /// EsslAttendanceSyncBackgroundService did not already clear it).
        /// Only ever resets the lock when it has been held for at least
        /// the same "stale lock" threshold SyncAsync's own automatic
        /// take-over uses - a genuinely active sync (lock age below that
        /// threshold) is left completely alone and this returns
        /// Success = false instead, so this can never be used to
        /// interrupt a real in-progress run.
        /// </summary>
        Task<(bool Success, string Message)> ResetStuckSyncAsync(string tenantId);
    }
}
