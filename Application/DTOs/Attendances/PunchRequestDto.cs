using System;
using System.Collections.Generic;
using System.Text;
using static Domain.Enums.EnumExtensions;

namespace Application.DTOs.Attendance
{
    public class PunchRequestDto
    {
        public string EmployeeId { get; set; }
        public DateTime PunchTime { get; set; }

        public string? Browser { get; set; }
        public string? Version { get; set; }
        public string? OS { get; set; }
        public string? DeviceType { get; set; }
        public bool IsManual { get; set; } = false;
        public string? Location { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public string? ModifiedBy { get; set; }
    }
}
