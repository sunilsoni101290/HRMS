using System;

namespace Infrastructure.EsslIntegration
{
    /// <summary>
    /// Read-only mapping onto eTimeTrackLite1's [dbo].[DeviceLogs] table
    /// shape - the SAME column layout is assumed for every monthly
    /// DeviceLogs_M_YYYY partition table too (that is how eTimeTrackLite1's
    /// own monthly-archive feature creates them: a straight copy of
    /// DeviceLogs' schema). Column set intentionally excludes EmployeeImage
    /// (image blob) and Longitude/Latitude/LocationAddress - not needed for
    /// attendance import and would otherwise force a heavy SELECT.
    ///
    /// Populated two ways depending on which physical table this row came
    /// from:
    ///  - Rows read via EsslDbContext.DeviceLogs (EF, only ever used for the
    ///    single-table "Test Connection" sample query) map 1:1 onto real
    ///    columns as usual.
    ///  - Rows read via EsslAttendanceDataSource.GetDeviceLogsAsync (the
    ///    actual sync path, which UNIONs across DeviceLogs plus zero or more
    ///    discovered DeviceLogs_M_YYYY tables using raw ADO.NET, since EF
    ///    Core cannot map one entity onto a dynamically-discovered set of
    ///    tables) also populate SourceTable - see EsslDeviceLogCursor's
    ///    remarks for why that matters for idempotency and pagination once
    ///    more than one physical table is involved.
    ///
    /// DeviceLogId is an IDENTITY column - unique across ONE physical table,
    /// but NOT globally unique once monthly partition tables exist (each
    /// partition has its own identity sequence, so DeviceLogId=91 can appear
    /// in both DeviceLogs_8_2026 and DeviceLogs_9_2026). The sync service
    /// therefore keys idempotency off (SourceTable, DeviceLogId) together for
    /// any row that did not come from the bare "DeviceLogs" table - see
    /// EsslAttendanceSyncService's DeviceTransactionId construction.
    /// </summary>
    public class EsslDeviceLogRaw
    {
        public int DeviceLogId { get; set; }

        public int DeviceId { get; set; }

        /// <summary>
        /// The device-side employee identifier for this punch. Matches
        /// [dbo].[Employees].[EmployeeCodeInDevice] in the same eTimeTrackLite1
        /// database (per the supplied esslEmployeeAdd.sql schema) - which is
        /// also what HRMS's existing EmployeeBiometricMapping.BiometricEmployeeCode
        /// is expected to be populated with for eSSL-sourced mappings.
        /// </summary>
        public string UserId { get; set; } = "";

        /// <summary>
        /// THE actual punch date/time - when the employee physically punched
        /// the biometric device. This is what attendance processing must use
        /// (see class remarks on DownloadDate below for why the two are NOT
        /// interchangeable).
        /// </summary>
        public DateTime LogDate { get; set; }

        /// <summary>
        /// When eTimeTrackLite1 downloaded/imported this punch from the
        /// device into its own database - NOT the punch time. A device that
        /// was offline/unreachable for days (or a device with punches
        /// downloaded manually much later) can show a LogDate of, say,
        /// 2026-08-04 with a DownloadDate of 2026-08-31 - the punch is still
        /// an August 4th attendance event. Used only for audit/troubleshooting
        /// ("when did this row actually arrive in eTimeTrackLite1") - never
        /// for selecting the sync window or for attendance-date logic, both
        /// of which key off LogDate exclusively.
        /// </summary>
        public DateTime? DownloadDate { get; set; }

        /// <summary>
        /// Raw direction value as eTimeTrackLite1 stored it - do NOT assume
        /// a fixed vocabulary ("IN"/"OUT"/"I"/"O"/etc. have all been seen in
        /// real eSSL deployments). See EsslAttendanceSyncService.ResolvePunchType
        /// for the actual normalization logic, which treats this as
        /// untrusted free text.
        /// </summary>
        public string? Direction { get; set; }

        /// <summary>
        /// A second, sometimes-populated direction-like field some eSSL
        /// software versions use instead of (or in addition to) Direction.
        /// Checked as a fallback when Direction is null/unrecognized.
        /// </summary>
        public string? AttDirection { get; set; }

        public string? WorkCode { get; set; }

        // C1-C7: reserved/vendor-specific columns present on eTimeTrackLite1's
        // DeviceLogs schema (exact business meaning not documented by eSSL
        // and not required for attendance processing - IN/OUT is driven by
        // Direction/AttDirection only, per the integration requirement's
        // explicit instruction not to invent meaning for undocumented
        // fields). Captured here only so they are available for audit/export
        // and so a future requirement can use them without another schema
        // change. Read via CONVERT(nvarchar(50), ...) in the raw SQL (see
        // EsslAttendanceDataSource) so an unknown underlying column type
        // (int/bit/varchar all seen across eSSL software versions) can never
        // break the query.
        public string? C1 { get; set; }
        public string? C2 { get; set; }
        public string? C3 { get; set; }
        public string? C4 { get; set; }
        public string? C5 { get; set; }
        public string? C6 { get; set; }
        public string? C7 { get; set; }

        /// <summary>
        /// Which physical table this row was read from - "DeviceLogs" or a
        /// discovered "DeviceLogs_M_YYYY" partition. Not a real column in
        /// eTimeTrackLite1 - set by the reader (EsslAttendanceDataSource)
        /// from the table it queried. Required for correct idempotency and
        /// pagination once more than one physical table can be involved -
        /// see the class remarks above and EsslDeviceLogCursor.
        /// </summary>
        public string SourceTable { get; set; } = EsslDeviceLogTableName.BaseTableName;
    }
}
