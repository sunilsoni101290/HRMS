using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using static Domain.Enums.EnumExtensions;

namespace Domain.Entities
{
    public class AttendanceLog : BaseEntity
    {
        [Required]
        public string AttendanceId { get; set; }
        public virtual Attendance Attendance { get; set; }

        [Required]
        public string EmployeeId { get; set; }

        [Required]
        public DateTime PunchTime { get; set; }

        public PunchType PunchType { get; set; }
        // IN / OUT / BREAK_IN / BREAK_OUT

        public string? DeviceId { get; set; }
        public string? Location { get; set; }

        public bool IsManual { get; set; } = false;

        public override string GetSequencePrefix() => "ATL";
    }
}
