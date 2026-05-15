using System;
using System.Collections.Generic;
using System.Text;
using static Domain.Enums.EnumExtensions;

namespace Application.DTOs.Attendance
{
    public class PunchRequestDto
    {
        public string EmployeeId { get; set; }
        public DateTime PunchTime { get; set; } = DateTime.UtcNow;

        public string? DeviceId { get; set; }
        public string? Location { get; set; }
    }
}
