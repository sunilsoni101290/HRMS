using System;
using System.ComponentModel.DataAnnotations;

namespace Domain.Entities
{
    /// <summary>
    /// Persistent "cursor" for the eSSL eTimeTrackLite1 direct-SQL attendance
    /// sync (see Application/Services/Attendances/EsslAttendanceSyncService.cs).
    /// One row per tenant (upserted, never duplicated) - NOT a per-run log,
    /// see BiometricSyncLog for that (reused here via SyncType =
    /// "EsslDbPull", extended with FromDate/ToDate/TriggeredBy).
    ///
    /// Why a real row instead of just "MAX(DeviceLogId) already imported"
    /// computed on the fly: the app must recover correctly after a restart/
    /// crash mid-batch (requirement: "the sync must recover correctly after
    /// application restart, IIS restart, server restart, SQL connection
    /// failure, partial processing"). Deriving the cursor from
    /// BiometricAttendanceLog after a partial failure would silently skip
    /// whatever the failed run didn't manage to insert - it should keep
    /// retrying that window instead. This is deliberately NOT relied on as
    /// the ONLY duplicate guard, though - see EsslAttendanceSyncService's
    /// query window (LastProcessedLogDate MINUS a rolling overlap, never a
    /// bare "> LastProcessedDeviceLogId") plus the real DB-level unique
    /// indexes on BiometricAttendanceLog (already existing:
    /// IX_BiometricAttendanceLogs_Tenant_Device_Employee_PunchTime and
    /// IX_BiometricAttendanceLogs_Device_TransactionId) - those are what
    /// actually make re-processing an overlapping window safe. This table
    /// only ever needs to be "close enough" to keep each run's SELECT window
    /// small; correctness against duplicates never depends on it alone.
    /// </summary>
    public class EsslAttendanceSyncState : BaseEntity
    {
        /// <summary>The last eTimeTrackLite1 DeviceLogs.DeviceLogId successfully imported (globally unique IDENTITY column in that table, safe as a monotonic watermark).</summary>
        public int? LastProcessedDeviceLogId { get; set; }

        /// <summary>LogDate of the last successfully imported row - the query window floor for the next run is this MINUS the configured overlap, not this exact value, so late-arriving/corrected rows with an earlier LogDate than the current watermark are still picked up.</summary>
        public DateTime? LastProcessedLogDate { get; set; }

        /// <summary>
        /// Which physical eTimeTrackLite1 table (e.g. "DeviceLogs" or
        /// "DeviceLogs_8_2026") LastProcessedDeviceLogId came from -
        /// informational/audit only. Once monthly DeviceLogs_M_YYYY
        /// partition tables are involved, DeviceLogId alone is only unique
        /// WITHIN one physical table (see EsslDeviceLogRaw's remarks), so
        /// this column exists purely so the Settings screen/troubleshooting
        /// can show an unambiguous watermark - it is never used as the sole
        /// basis for the next run's query window (LastProcessedLogDate minus
        /// the configured overlap is), and it is never used for duplicate
        /// detection either (the DB unique index on BiometricAttendanceLog is).
        /// </summary>
        [MaxLength(60)]
        public string? LastProcessedSourceTable { get; set; }

        public DateTime? LastSyncStartedAt { get; set; }

        public DateTime? LastSyncCompletedAt { get; set; }

        [MaxLength(20)]
        public string? LastSyncStatus { get; set; }

        [MaxLength(1000)]
        public string? LastError { get; set; }

        public int RecordsRead { get; set; }

        public int RecordsImported { get; set; }

        public int RecordsSkipped { get; set; }

        public int RecordsFailed { get; set; }

        /// <summary>
        /// True while a sync run (automatic or manual) is actively in
        /// progress for this tenant - a lightweight app-level lock so the
        /// background service and a manually-triggered run can never process
        /// the same batch concurrently (requirement #12: "use appropriate
        /// locking so two sync processes cannot process the same batch
        /// simultaneously"). Cleared in a finally block; EsslAttendanceSyncService
        /// also treats a lock held longer than StaleLockMinutes as abandoned
        /// (e.g. the process crashed mid-run) and takes over rather than
        /// deadlocking the feature forever.
        /// </summary>
        public bool IsSyncRunning { get; set; }

        public override string GetSequencePrefix() => "ESS";
    }
}
