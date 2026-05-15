using static APP.Helpers.EnumExtensions;

namespace APP.Models.DTOs
{
    public class AttendanceLogDto
    {
        public string? Id { get; set; }

        public string AttendanceId { get; set; }

        public string EmployeeId { get; set; }

        public string? EmployeeName { get; set; }

        public DateTime PunchTime { get; set; }

        public string PunchType { get; set; }

        public string? DeviceId { get; set; }

        public string? Location { get; set; }

        public bool IsManual { get; set; }

        public DateTime CreatedDate { get; set; }
    }
}
