using System;
using System.Collections.Generic;

namespace Application.DTOs.Attendances
{
    // Aggregate for AttendanceService.GetDashboardAsync - today's org-wide
    // attendance snapshot plus a 30-day trend and department breakdown.
    public class AttendanceDashboardDto
    {
        public int PresentToday { get; set; }
        public int AbsentToday { get; set; }
        public int LateToday { get; set; }
        public int OnLeaveToday { get; set; }

        public int TotalActiveEmployees { get; set; }

        public decimal PresentPercentToday { get; set; }

        // Count of AttendanceRegularizations with Status == Pending.
        public int PendingRegularizations { get; set; }

        public List<AttendanceTrendPointDto> Last30DaysTrend { get; set; } = new();

        public List<DepartmentAttendanceDto> DepartmentWisePresentToday { get; set; } = new();
    }

    public class AttendanceTrendPointDto
    {
        public DateTime Date { get; set; }
        public int PresentCount { get; set; }
        public int AbsentCount { get; set; }
        public int LateCount { get; set; }
    }

    public class DepartmentAttendanceDto
    {
        public string DepartmentName { get; set; }
        public int PresentCount { get; set; }
        public int TotalCount { get; set; }
        public decimal PresentPercent { get; set; }
    }
}
