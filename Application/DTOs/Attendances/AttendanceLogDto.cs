using System;
using System.Collections.Generic;
using System.Text;
using static Domain.Enums.EnumExtensions;

namespace Application.DTOs.Attendances
{
    public class AttendanceLogDto
    {
        public string? Id { get; set; }

        public string AttendanceId { get; set; }

        public string EmployeeId { get; set; }
        public string? EmployeeName { get; set; }
        public DateTime PunchTime { get; set; }
        public string PunchType { get; set; }
        public string? Browser { get; set; }
        public string? Version { get; set; }
        public string? OS { get; set; }
        public string? DeviceType { get; set; }
        public string? Location { get; set; }
        public bool IsManual { get; set; }

        public DateTime CreatedDate { get; set; }
    }
}
