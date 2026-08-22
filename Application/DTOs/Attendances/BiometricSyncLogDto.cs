using System;

namespace Application.DTOs.Attendances
{
    public class BiometricSyncLogDto
    {
        public string Id { get; set; }
        public string? DeviceId { get; set; }
        public string? DeviceCode { get; set; }
        public string? DeviceName { get; set; }
        public string SyncType { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int RecordsFetched { get; set; }
        public int RecordsInserted { get; set; }
        public int RecordsSkipped { get; set; }
        public int RecordsFailed { get; set; }
        public string Status { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>Aggregate figures for the Device Health dashboard's summary cards.</summary>
    public class BiometricDashboardSummaryDto
    {
        public int TotalDevices { get; set; }
        public int OnlineDevices { get; set; }
        public int OfflineDevices { get; set; }
        public int DisabledDevices { get; set; }
        public int TodaysPunchCount { get; set; }
        public int PendingPunchCount { get; set; }
        public int FailedSyncCount24h { get; set; }
        public DateTime? LastSuccessfulSync { get; set; }
    }
}
