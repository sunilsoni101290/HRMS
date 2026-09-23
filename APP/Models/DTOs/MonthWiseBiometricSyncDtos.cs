using System;

namespace APP.Models.DTOs
{
    // Mirrors Application/DTOs/Attendances/MonthWiseBiometricSyncDtos.cs -
    // kept as a separate APP-side type set per this codebase's existing
    // convention (see HistoricalAttendanceSyncDtos.cs's own remarks), not
    // shared across the API/APP project boundary.

    public class MonthWiseSyncRequestDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string? TenantId { get; set; }
        public string CompanyId { get; set; } = "";
        public string ShiftId { get; set; } = "";
        public string? BranchId { get; set; }
        public string CreatedBy { get; set; } = "SYSTEM";
        public int Status { get; set; } = 0;
    }

    public class MonthWiseSyncResultDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int AttendancesInserted { get; set; }
        public int AttendanceLogsInserted { get; set; }
        public int SkippedRecords { get; set; }
        public string SyncStatus { get; set; } = "";
    }
}
