using System;
using System.Collections.Generic;
using System.Text;
using static Domain.Enums.EnumExtensions;

namespace Application.DTOs.Attendances
{
    public class BiometricAttendanceLogDto
    {
        public string? Id { get; set; }

        public string DeviceId { get; set; }

        public string BiometricEmployeeCode { get; set; }

        public DateTime PunchTime { get; set; }

        public PunchType PunchType { get; set; }

        public bool IsProcessed { get; set; }
    }
}
