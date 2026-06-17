using System;
using System.Collections.Generic;
using System.Text;
using static Domain.Enums.EnumExtensions;

namespace Application.DTOs.Attendances
{
    public class AttendanceProcessDto
    {
        public string EmployeeId { get; set; }

        public DateTime AttendanceDate { get; set; }

        public DateTime? FirstIn { get; set; }

        public DateTime? LastOut { get; set; }

        public decimal WorkingHours { get; set; }

        public decimal BreakHours { get; set; }

        public decimal OvertimeHours { get; set; }

        public AttendanceStatus Status { get; set; }
    }
}
