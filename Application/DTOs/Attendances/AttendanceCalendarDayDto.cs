using System;

namespace Application.DTOs.Attendances
{
    // One entry per calendar day for AttendanceService.GetCalendarAsync.
    // Status uses the same classification values as the ESS dashboard's
    // Week Attendance strip - see Domain/Helper/AttendanceStatusHelper.cs.
    public class AttendanceCalendarDayDto
    {
        public DateTime Date { get; set; }

        public string Status { get; set; }

        public TimeSpan? FirstIn { get; set; }
        public TimeSpan? LastOut { get; set; }

        public decimal? TotalWorkingHours { get; set; }

        public string? ShiftName { get; set; }

        // True if an Approved AttendanceRegularization exists for this
        // employee/date.
        public bool IsRegularized { get; set; }
    }
}
