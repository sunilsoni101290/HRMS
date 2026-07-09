namespace APP.Models.DTOs
{
    public class BiometricDeviceHealthDto
    {
        public string DeviceId { get; set; }
        public string DeviceName { get; set; }
        public string DeviceCode { get; set; }
        public string IPAddress { get; set; }
        public bool IsActive { get; set; }
        public DateTime? LastSyncDate { get; set; }
        public bool IsOnline { get; set; }
        public int TotalPunchCount { get; set; }
        public int PendingPunchCount { get; set; }
    }
}
