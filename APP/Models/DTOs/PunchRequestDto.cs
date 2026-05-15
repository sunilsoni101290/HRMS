namespace APP.Models.DTOs
{
    public class PunchRequestDto
    {
        public string EmployeeId { get; set; }
        public DateTime PunchTime { get; set; } = DateTime.UtcNow;
        public string? DeviceId { get; set; }
        public string? Location { get; set; }
    }
}
