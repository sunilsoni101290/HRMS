namespace APP.Models.DTOs
{
    public class PunchRequestDto
    {
        public string EmployeeId { get; set; }
        public DateTime PunchTime { get; set; } = DateTime.UtcNow;
        public string? Browser { get; set; }
        public string? Version { get; set; }
        public string? OS { get; set; }
        public string? DeviceType { get; set; }
        public string? Location { get; set; }
        public bool IsManual { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public string? ModifiedBy { get; set; }

    }
}
