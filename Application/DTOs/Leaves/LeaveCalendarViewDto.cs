using System;
using System.Collections.Generic;

namespace Application.DTOs.Leaves
{
    // One approved leave span that falls (fully or partially) inside the
    // requested month - the Calendar view spreads this across every day
    // cell between FromDate and ToDate that lands in that month.
    public class LeaveCalendarEntryDto
    {
        public string LeaveApplicationId { get; set; }

        public string EmployeeId { get; set; }
        public string EmployeeName { get; set; }

        public string? LeaveTypeName { get; set; }

        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }

        public bool IsHalfDay { get; set; }
    }

    public class LeaveCalendarResponseDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string MonthName { get; set; }

        // Approved leaves overlapping the month, scoped per the caller
        // (org-wide for admin, own-department for a self-service employee).
        public List<LeaveCalendarEntryDto> Entries { get; set; } = new();

        // Optional overlay - tenant holiday dates inside the month, reusing
        // the same IWeekOffService.IsHoliday check the day-count calculator
        // already uses. Best-effort: left empty if no tenant is resolvable.
        public List<DateTime> HolidayDates { get; set; } = new();
    }
}
