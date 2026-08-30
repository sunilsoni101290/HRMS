using System;

namespace Infrastructure.EsslIntegration
{
    /// <summary>
    /// Read-only mapping onto the eTimeTrackLite1 eSSL software's own
    /// [dbo].[DeviceLogs] table (exact schema per the supplied
    /// esslDevicelogs.sql - NOT guessed). This is the raw biometric punch
    /// source (requirement #3). Column set intentionally excludes
    /// EmployeeImage (image blob) and Longitude/Latitude/LocationAddress -
    /// not needed for attendance import and would otherwise force a heavy
    /// SELECT (requirement #26: never SELECT * against this table).
    ///
    /// DeviceLogId is an IDENTITY column - unique across the WHOLE table on
    /// its own (SQL Server guarantees this regardless of what the table's
    /// declared PRIMARY KEY actually covers - here it's a composite
    /// (DeviceLogId, UserId, LogDate)). That makes it a safe, simple,
    /// globally-unique idempotency key for
    /// BiometricAttendanceLog.DeviceTransactionId - no need to compose it
    /// with DeviceId.
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

        public DateTime LogDate { get; set; }

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
    }
}
