namespace APP.Models.DTOs
{
    /// <summary>Mirrors Application.DTOs.Attendances.BiometricDeviceStatusDto - response of GET /api/BiometricDevice/{id}/status.</summary>
    public class BiometricDeviceStatusDto
    {
        public string DeviceId { get; set; }
        public string DeviceCode { get; set; }
        public string DeviceName { get; set; }
        public bool IsActive { get; set; }
        public bool IsOnline { get; set; }
        public DateTime? LastSyncDate { get; set; }
        public DateTime? LastSeen { get; set; }
        public string? AgentId { get; set; }
        public string? AgentCode { get; set; }
        public string? AgentName { get; set; }
        public bool? AgentOnline { get; set; }
        public DateTime? AgentLastHeartbeat { get; set; }
        public int TotalPunchCount { get; set; }
        public int PendingPunchCount { get; set; }
        public string? LastError { get; set; }
    }
}
