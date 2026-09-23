using System;

namespace Application.DTOs.Attendances
{
    /// <summary>
    /// Request for the standalone "Sync Biometric Attendance (Month Wise)"
    /// admin tool - see Application/Services/Attendances/
    /// MonthWiseBiometricSyncService.cs's remarks. TenantId here is only
    /// informational (the caller/API always uses ITenantService.GetTenantId()
    /// as the authoritative value, same convention as
    /// HistoricalSyncRequestDto/HistoricalAttendanceSyncController); it is
    /// kept on the DTO only so the value round-trips through the Preview/
    /// Execute JSON payload for the UI's own display purposes.
    /// </summary>
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

    /// <summary>
    /// Mirrors the single result row returned by
    /// dbo.usp_SyncBiometricAttendance_MonthWise's final SELECT - read via
    /// SqlDataReader (a result set, not OUTPUT parameters).
    /// </summary>
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
