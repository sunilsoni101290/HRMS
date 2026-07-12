using static APP.Helpers.EnumExtensions;

namespace APP.Models.DTOs
{
    public class LoginHistoryListDto
    {
        public string Id { get; set; }
        public string UserId { get; set; }
        public string TenantId { get; set; }
        public string? Username { get; set; }
        public string? EmployeeName { get; set; }
        public DateTime LoginTime { get; set; }
        public DateTime? LogoutTime { get; set; }
        public LoginStatus LoginStatus { get; set; }
        public string? FailureReason { get; set; }
        public string? IPAddress { get; set; }
        public string? DeviceInfo { get; set; }
        public string? Browser { get; set; }
        public string? OS { get; set; }
        public bool IsSuspicious { get; set; } = false;
    }
}
