using System.ComponentModel.DataAnnotations;
using static APP.Helpers.EnumExtensions;

namespace APP.Models.DTOs
{
    public class AttendanceLogDto
    {
        public string? Id { get; set; }

        public string AttendanceId { get; set; }

        public string EmployeeId { get; set; }

        public string? EmployeeName { get; set; }
        public string? Browser { get; set; }
        public string? Version { get; set; }
        public string? OS { get; set; }
        public string? DeviceType { get; set; }
        public string? Location { get; set; }
        public DateTime PunchTime { get; set; }
        public string PunchType { get; set; }
        public bool IsManual { get; set; }

        // Biometric source (null for web/manual punches)
        public string? DeviceId { get; set; }
        public string? DeviceName { get; set; }

        public DateTime CreatedDate { get; set; }
    }
    public class DeviceInfo
    {
        public string Browser { get; set; }
        public string Version { get; set; }
        public string OS { get; set; }
        public string DeviceType { get; set; }
    }
    
}
