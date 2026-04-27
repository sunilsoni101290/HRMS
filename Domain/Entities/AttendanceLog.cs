using System;
using System.Collections.Generic;
using System.Text;
using static Domain.Enums.EnumExtensions;

namespace Domain.Entities
{
    public class AttendanceLog : BaseEntity
    {
        public string EmployeeId { get; set; }
        public virtual Employee Employee { get; set; }

        public DateTime PunchTime { get; set; }

        public PunchType PunchType { get; set; } // IN / OUT

        public string DeviceId { get; set; } // Biometric machine
        public override string GetSequencePrefix() => "ATL";
    }
}
