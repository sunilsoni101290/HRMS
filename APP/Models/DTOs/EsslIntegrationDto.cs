using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace APP.Models.DTOs
{
    // Mirrors Application/DTOs/Attendances/EsslIntegrationDto.cs - kept as a
    // separate APP-side type per this codebase's existing convention (see
    // ErrorLogDto/BiometricAgentDto etc.), not shared across the API/APP
    // project boundary.

    /// <summary>Card 1 "Integration Status" - runtime/status only, never editable.</summary>
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

    /// <summary>Card 2 "Database Configuration" GET - never contains a password.</summary>
    public class EsslDatabaseConfigViewDto
    {
        public bool IntegrationEnabled { get; set; }
        public string DatabaseServer { get; set; } = "";
        public string DatabaseName { get; set; } = "etimetracklite1";
        public string AuthenticationType { get; set; } = "Sql";
        public string? Username { get; set; }
        public bool HasPasswordConfigured { get; set; }
        public int ConnectionTimeout { get; set; } = 15;
        public int SyncIntervalMinutes { get; set; } = 5;
        public int BatchSize { get; set; } = 500;
    }

    /// <summary>POST body for both Save Settings and Test Connection - blank/null Password means "keep/use the existing saved password".</summary>
    public class EsslDatabaseConfigDto
    {
        public bool IntegrationEnabled { get; set; }

        [Required(ErrorMessage = "Database server is required.")]
        public string DatabaseServer { get; set; } = "";

        [Required(ErrorMessage = "Database name is required.")]
        public string DatabaseName { get; set; } = "etimetracklite1";

        [Required(ErrorMessage = "Authentication type is required.")]
        public string AuthenticationType { get; set; } = "Sql";

        public string? Username { get; set; }

        public string? Password { get; set; }

        [Range(1, 300, ErrorMessage = "Connection timeout must be between 1 and 300 seconds.")]
        public int ConnectionTimeout { get; set; } = 15;

        [Range(1, 1440, ErrorMessage = "Sync interval must be between 1 and 1440 minutes.")]
        public int SyncIntervalMinutes { get; set; } = 5;

        [Range(1, 5000, ErrorMessage = "Batch size must be between 1 and 5000 records.")]
        public int BatchSize { get; set; } = 500;
    }

    public class EsslSyncRequestDto
    {
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

    public class EsslUnmappedEmployeeDto
    {
        public string BiometricEmployeeCode { get; set; } = "";
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

    /// <summary>Composite view model for the Settings tab - Card 1 (status) + Card 2 (editable configuration) together.</summary>
    public class EsslSettingsPageViewModel
    {
        public EsslSyncSettingsDto Status { get; set; } = new();
        public EsslDatabaseConfigViewDto Config { get; set; } = new();
    }
}
