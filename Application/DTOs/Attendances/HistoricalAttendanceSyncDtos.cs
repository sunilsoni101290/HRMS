using System;
using System.Collections.Generic;

namespace Application.DTOs.Attendances
{
    /// <summary>
    /// DTOs for the standalone Historical Attendance Sync feature - see
    /// Domain/Entities/HistoricalAttendanceSyncJob.cs and
    /// IHistoricalAttendanceSyncService.cs. Deliberately its own file/type
    /// set, never reusing EsslIntegrationDto.cs's eSSL-specific DTOs (which
    /// stay untouched - see the "out of scope" list in the feature spec).
    /// </summary>

    /// <summary>Request body for POST HistoricalAttendanceSync/preview and /start.</summary>
    public class HistoricalSyncRequestDto
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }

        /// <summary>Optional - null/empty means every employee with an active biometric mapping.</summary>
        public string? EmployeeId { get; set; }

        /// <summary>0/negative falls back to the SP's own default (1000).</summary>
        public int BatchSize { get; set; } = 1000;
    }

    /// <summary>Read-only preview of what a given date range would touch - never writes anything.</summary>
    public class HistoricalSyncPreviewDto
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string? EmployeeId { get; set; }
        public string? EmployeeName { get; set; }

        /// <summary>Rows in BiometricAttendanceLogs within the window (tenant + optional employee scoped), regardless of processed state.</summary>
        public int TotalBiometricLogsInRange { get; set; }

        /// <summary>Of those, rows with no AttendanceLogs.BiometricAttendanceLogId link yet - what a Start Historical Sync run would actually attempt.</summary>
        public int UnprocessedLogsInRange { get; set; }

        /// <summary>Already linked to an AttendanceLog (from a previous historical run or the live/eSSL processing path) - will be skipped as duplicates.</summary>
        public int AlreadyLinkedCount { get; set; }

        /// <summary>BiometricAttendanceLogs.IsDuplicate = 1 rows in range - will be skipped, never re-inserted.</summary>
        public int FlaggedDuplicateCount { get; set; }

        /// <summary>Distinct EmployeeCode values in range with no active EmployeeBiometricMapping (normalized match) - these will be counted as Unmapped by a run.</summary>
        public int UnmappedEmployeeCodeCount { get; set; }

        public List<string> SampleUnmappedCodes { get; set; } = new();

        /// <summary>True if a Queued/Running Historical Sync job already exists for this tenant with an overlapping date range - Start would be rejected.</summary>
        public bool HasOverlappingActiveJob { get; set; }

        public string? OverlappingJobId { get; set; }
    }

    /// <summary>Response for POST HistoricalAttendanceSync/start - the job is enqueued and this returns immediately, per spec section 6.</summary>
    public class HistoricalSyncStartResultDto
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public string? JobId { get; set; }
    }

    /// <summary>Full status row - backs the UI's status card and the job history list. Mirrors HistoricalAttendanceSyncJob 1:1.</summary>
    public class HistoricalSyncJobDto
    {
        public string Id { get; set; } = "";
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string? EmployeeId { get; set; }
        public string? EmployeeName { get; set; }
        public int BatchSize { get; set; }
        public string RequestedBy { get; set; } = "";

        /// <summary>Queued / Running / Completed / Failed / PartiallyCompleted.</summary>
        public string Status { get; set; } = "Queued";

        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public double? DurationSeconds { get; set; }

        public int TotalRecords { get; set; }
        public int ProcessedCount { get; set; }
        public int FailedCount { get; set; }
        public int UnmappedCount { get; set; }
        public int DuplicateCount { get; set; }
        public int AttendanceLogsCreated { get; set; }
        public int AttendancesCreated { get; set; }
        public int AttendancesUpdated { get; set; }

        public string? ErrorSummary { get; set; }

        public DateTime CreatedOn { get; set; }
    }

    /// <summary>Filter for the job history list.</summary>
    public class HistoricalSyncJobFilterDto
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public string? Status { get; set; }
    }
}
