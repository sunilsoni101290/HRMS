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
        public virtual Employee Employee { get; set; }

        [Required]
        public DateTime PunchTime { get; set; }

        public PunchType PunchType { get; set; }  // IN / OUT / BREAK_IN / BREAK_OUT

        public string? Browser { get; set; }
        public string? Version { get; set; }
        public string? OS { get; set; }
        public string? DeviceType { get; set; }

        public string? Location { get; set; }

        public bool IsManual { get; set; } = false;
        // Biometric
        public string? DeviceId { get; set; }

        public string? BiometricCode { get; set; }

        // Links this log back to the exact raw BiometricAttendanceLog row
        // that produced it (spec: "Attendances -> AttendanceLogs ->
        // BiometricAttendanceLogs" traceability, and the idempotency guard
        // in AttendanceProcessorService.ProcessAttendanceAsync - "has this
        // raw punch already been applied?" is answered by looking this up
        // rather than a fragile EmployeeId+PunchTime match alone). Null for
        // logs created by the manual/web punch clock (IsManual = true) or
        // any log created before this column existed.
        public string? BiometricAttendanceLogId { get; set; }
        public virtual BiometricAttendanceLog? BiometricAttendanceLog { get; set; }

        public override string GetSequencePrefix() => "ATL";
    }
}
