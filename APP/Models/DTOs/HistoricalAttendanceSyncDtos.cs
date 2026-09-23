using System;
using System.Collections.Generic;

namespace APP.Models.DTOs
{
    // Mirrors Application/DTOs/Attendances/HistoricalAttendanceSyncDtos.cs -
    // kept as a separate APP-side type set per this codebase's existing
    // convention (see EsslIntegrationDto.cs's own remarks), not shared
    // across the API/APP project boundary.

    public class HistoricalSyncRequestDto
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string? EmployeeId { get; set; }
        public int BatchSize { get; set; } = 1000;
    }

    public class HistoricalSyncPreviewDto
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string? EmployeeId { get; set; }
        public string? EmployeeName { get; set; }
        public int TotalBiometricLogsInRange { get; set; }
        public int UnprocessedLogsInRange { get; set; }
        public int AlreadyLinkedCount { get; set; }
        public int FlaggedDuplicateCount { get; set; }
        public int UnmappedEmployeeCodeCount { get; set; }
        public List<string> SampleUnmappedCodes { get; set; } = new();
        public bool HasOverlappingActiveJob { get; set; }
        public string? OverlappingJobId { get; set; }
    }

    public class HistoricalSyncStartResultDto
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public string? JobId { get; set; }
    }

    public class HistoricalSyncJobDto
    {
        public string Id { get; set; } = "";
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string? EmployeeId { get; set; }
        public string? EmployeeName { get; set; }
        public int BatchSize { get; set; }
        public string RequestedBy { get; set; } = "";
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

    public class HistoricalSyncJobFilterDto
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public string? Status { get; set; }
    }
}
