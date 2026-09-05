using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Attendances
{
    /// <summary>
    /// Runtime/status information ONLY (Card 1, "Integration Status") - never
    /// editable. Configuration fields (server, database, credentials, sync
    /// interval, batch size) live in EsslDatabaseConfigViewDto/EsslDatabaseConfigDto
    /// below instead, per the Settings-page redesign that separates status
    /// from configuration.
    /// </summary>
    public class EsslSyncSettingsDto
    {
        public bool Enabled { get; set; }

        public bool IsSyncRunning { get; set; }

        public DateTime? LastSyncStartedAt { get; set; }

        public DateTime? LastSyncCompletedAt { get; set; }

        public string? LastSyncStatus { get; set; }

        public string? LastError { get; set; }

        public int? LastProcessedDeviceLogId { get; set; }

        public DateTime? LastProcessedLogDate { get; set; }
    }

    /// <summary>
    /// GET response for the editable "Database Configuration" card - deliberately
    /// has NO password property at all (not even masked/empty - the property
    /// simply does not exist on this type), so there is no code path by which
    /// a password could accidentally be serialized back to the browser.
    /// HasPasswordConfigured tells the UI whether to show "a password is
    /// already saved" placeholder text.
    /// </summary>
    public class EsslDatabaseConfigViewDto
    {
        public bool IntegrationEnabled { get; set; }

        public string DatabaseServer { get; set; } = "";

        public string DatabaseName { get; set; } = "etimetracklite1";

        /// <summary>"Sql" or "Windows" - see Domain.Helper.EsslAuthenticationTypes.</summary>
        public string AuthenticationType { get; set; } = "Sql";

        public string? Username { get; set; }

        public bool HasPasswordConfigured { get; set; }

        public int ConnectionTimeout { get; set; } = 15;

        public int SyncIntervalMinutes { get; set; } = 5;

        public int BatchSize { get; set; } = 500;
    }

    /// <summary>
    /// POST body for both Save Settings and Test Connection - the same shape
    /// for both, since Test Connection must "read the values currently
    /// entered in the form" (never the previously-saved ones) per the
    /// requirement. Password blank/null means "keep the existing saved
    /// password unchanged" for Save, or "use the existing saved password for
    /// this test" for Test Connection when one is already configured -
    /// never invented, never defaulted to empty-string-as-a-real-password.
    /// </summary>
    public class EsslDatabaseConfigDto
    {
        public bool IntegrationEnabled { get; set; }

        [Required(ErrorMessage = "Database server is required.")]
        [MaxLength(300)]
        public string DatabaseServer { get; set; } = "";

        [Required(ErrorMessage = "Database name is required.")]
        [MaxLength(200)]
        public string DatabaseName { get; set; } = "etimetracklite1";

        [Required(ErrorMessage = "Authentication type is required.")]
        public string AuthenticationType { get; set; } = "Sql";

        [MaxLength(200)]
        public string? Username { get; set; }

        /// <summary>Null/empty = keep the existing saved password unchanged (see class remarks) - never round-tripped from a GET response.</summary>
        public string? Password { get; set; }

        [Range(1, 300, ErrorMessage = "Connection timeout must be between 1 and 300 seconds.")]
        public int ConnectionTimeout { get; set; } = 15;

        [Range(1, 1440, ErrorMessage = "Sync interval must be between 1 and 1440 minutes.")]
        public int SyncIntervalMinutes { get; set; } = 5;

        [Range(1, 5000, ErrorMessage = "Batch size must be between 1 and 5000 records.")]
        public int BatchSize { get; set; } = 500;
    }

    /// <summary>Request for both manual and historical sync - the SAME core service handles both (requirement: "must use the same core service, do not duplicate synchronization logic").</summary>
    public class EsslSyncRequestDto
    {
        /// <summary>Null = automatic incremental mode (background service). Non-null = manual/historical mode - an explicit window the admin chose.</summary>
        public DateTime? FromDate { get; set; }

        public DateTime? ToDate { get; set; }
    }

    public class EsslSyncResultDto
    {
        public bool Success { get; set; }

        public string? Message { get; set; }

        public int RecordsFound { get; set; }

        public int RecordsImported { get; set; }

        public int RecordsSkipped { get; set; }

        public int DuplicateCount { get; set; }

        public int UnknownEmployeeCount { get; set; }

        public int ErrorCount { get; set; }

        public double DurationSeconds { get; set; }

        /// <summary>
        /// The physical eTimeTrackLite1 tables this run actually scanned
        /// (e.g. ["DeviceLogs", "DeviceLogs_8_2026"]) - see
        /// EsslAttendanceDataSource.DiscoverDeviceLogTablesAsync. Empty means
        /// no matching table was found for the requested window (a
        /// misconfigured/unreachable database, or a historical import for a
        /// month that was never partitioned) - never silently treated the
        /// same as "found tables but they had zero rows".
        /// </summary>
        public List<string> TablesScanned { get; set; } = new();
    }

    public class EsslSyncHistoryDto
    {
        public string Id { get; set; } = "";

        public DateTime StartTime { get; set; }

        public DateTime? EndTime { get; set; }

        public DateTime? FromDate { get; set; }

        public DateTime? ToDate { get; set; }

        public int RecordsFetched { get; set; }

        public int RecordsInserted { get; set; }

        public int RecordsSkipped { get; set; }

        public int RecordsFailed { get; set; }

        public string Status { get; set; } = "";

        public string? ErrorMessage { get; set; }

        public string? TriggeredBy { get; set; }
    }

    /// <summary>One row per raw biometric employee code seen in eSSL punches with no active EmployeeBiometricMapping - requirement #21's "Unmapped Employees" admin report.</summary>
    public class EsslUnmappedEmployeeDto
    {
        public string BiometricEmployeeCode { get; set; } = "";

        /// <summary>Name from eTimeTrackLite1's own Employees table, when reachable - null falls back to showing just the code in the UI.</summary>
        public string? EsslEmployeeName { get; set; }

        public int PunchCount { get; set; }

        public DateTime FirstSeen { get; set; }

        public DateTime LastSeen { get; set; }
    }

    public class EsslSyncHistoryFilterDto
    {
        public DateTime? DateFrom { get; set; }

        public DateTime? DateTo { get; set; }

        public int PageNumber { get; set; } = 1;

        public int PageSize { get; set; } = 20;
    }
}
