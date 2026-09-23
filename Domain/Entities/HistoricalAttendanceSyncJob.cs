using System;
using System.ComponentModel.DataAnnotations;

namespace Domain.Entities
{
    /// <summary>
    /// One row per Historical Attendance Sync run - a COMPLETELY SEPARATE
    /// feature from the eSSL "Sync Now" / EsslAttendanceSyncState pipeline
    /// (see EsslAttendanceSyncState.cs). This never fetches anything from
    /// eSSL/any biometric device - it only ever re-processes rows that
    /// already exist in dbo.BiometricAttendanceLogs into
    /// AttendanceLogs/Attendances, for an admin-selected historical
    /// From/To date range (optionally one Employee), via
    /// dbo.ProcessHistoricalBiometricAttendance (see
    /// "Create HistoricalAttendanceSyncJob table and
    /// ProcessHistoricalBiometricAttendance SP.sql" at the repo root).
    ///
    /// Lock: IsRunning + the (TenantId, FromDate, ToDate) uniqueness check in
    /// HistoricalAttendanceSyncService.ClaimHistoricalSyncLockAsync is a
    /// DIFFERENT lock from EsslAttendanceSyncState.IsSyncRunning - a
    /// Historical Sync job never blocks, and is never blocked by, the eSSL
    /// automatic/"Sync Now" pipeline. Two Historical Sync jobs for the same
    /// tenant with OVERLAPPING date ranges cannot both be Queued/Running at
    /// once; jobs for disjoint ranges (or different tenants) run
    /// independently.
    /// </summary>
    public class HistoricalAttendanceSyncJob : BaseEntity
    {
        [Required]
        public new string TenantId { get; set; } = "";

        [Required]
        public DateTime FromDate { get; set; }

        [Required]
        public DateTime ToDate { get; set; }

        /// <summary>Optional single-employee filter - null means "every employee with a biometric mapping".</summary>
        public string? EmployeeId { get; set; }

        public int BatchSize { get; set; } = 1000;

        [Required]
        public string RequestedBy { get; set; } = "";

        /// <summary>Queued / Running / Completed / Failed / PartiallyCompleted.</summary>
        [MaxLength(30)]
        public string Status { get; set; } = "Queued";

        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// Count of BiometricAttendanceLogs rows found eligible for this
        /// job's TenantId/FromDate/ToDate/EmployeeId window (NOT EXISTS an
        /// AttendanceLogs.BiometricAttendanceLogId link - see the SP) at the
        /// time the run scanned each batch. Set incrementally as batches are
        /// scanned (the true total isn't known up-front without a second
        /// full table scan the SP intentionally avoids), and finalized once
        /// the run completes.
        /// </summary>
        public int TotalRecords { get; set; }

        /// <summary>Raw rows that produced a real AttendanceLog (+ Attendance create/update) this run - the only rows ever marked BiometricAttendanceLogs.IsProcessed = 1 by this feature.</summary>
        public int ProcessedCount { get; set; }

        public int FailedCount { get; set; }

        /// <summary>No active EmployeeBiometricMappings row for the punch's device code (normalized) at the time of processing.</summary>
        public int UnmappedCount { get; set; }

        /// <summary>Rows already linked to an AttendanceLog (by a previous historical run OR the live/eSSL processing path) or flagged BiometricAttendanceLogs.IsDuplicate - skipped, never re-inserted, never re-marked.</summary>
        public int DuplicateCount { get; set; }

        public int AttendanceLogsCreated { get; set; }
        public int AttendancesCreated { get; set; }
        public int AttendancesUpdated { get; set; }

        /// <summary>
        /// True while this job is actively executing - the Historical
        /// Sync's own lock flag, checked/claimed by
        /// HistoricalAttendanceSyncService.ClaimHistoricalSyncLockAsync
        /// together with the overlapping-date-range check. Cleared in a
        /// finally block; a row left true after an app restart is reconciled
        /// on startup by HistoricalAttendanceSyncBackgroundService, exactly
        /// like EsslAttendanceSyncState's equivalent flag.
        /// </summary>
        public bool IsRunning { get; set; }

        /// <summary>
        /// Human-readable rollup of what went wrong, if anything - one line
        /// per failed batch (batch-level errors are also written to the
        /// shared ErrorLog via IErrorLogService, this is just the
        /// job-scoped summary shown on the status screen). Never contains a
        /// raw stack trace.
        /// </summary>
        public string? ErrorSummary { get; set; }

        public override string GetSequencePrefix() => "HAJ";
    }
}
