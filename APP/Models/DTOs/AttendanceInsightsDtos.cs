namespace APP.Models.DTOs
{
    // Mirrors of Application.DTOs.Attendances.* backing
    // API/Controllers/AttendanceInsightsController.cs (api/attendanceinsights/*).
    // Property names/types must match the API's JSON 1:1.

    // One entry per calendar day - api/attendanceinsights/calendar
    public class AttendanceCalendarDayDto
    {
        public DateTime Date { get; set; }

        // Present, Late, HalfDay, Absent, Leave, Holiday, WeekOff,
        // WorkFromHome, OnDuty, Overtime, CompOff, EarlyExit, MissPunch, None
        public string Status { get; set; }

        public TimeSpan? FirstIn { get; set; }
        public TimeSpan? LastOut { get; set; }

        public decimal? TotalWorkingHours { get; set; }

        public string? ShiftName { get; set; }

        // True if an Approved AttendanceRegularization exists for this
        // employee/date.
        public bool IsRegularized { get; set; }
    }

    // One row per team member for a single date - api/attendanceinsights/team
    public class TeamAttendanceMemberDto
    {
        public string EmployeeId { get; set; }
        public string EmployeeName { get; set; }
        public string EmployeeCode { get; set; }

        public string? DepartmentName { get; set; }
        public string? DesignationName { get; set; }

        public string Status { get; set; }

        public TimeSpan? FirstIn { get; set; }
        public TimeSpan? LastOut { get; set; }

        public decimal? TotalWorkingHours { get; set; }
    }

    // One row per employee for a month/year - api/attendanceinsights/summary
    public class AttendanceSummaryRowDto
    {
        public string EmployeeId { get; set; }
        public string EmployeeName { get; set; }
        public string EmployeeCode { get; set; }

        public string? DepartmentName { get; set; }

        public int PresentDays { get; set; }
        public int AbsentDays { get; set; }
        public int LateDays { get; set; }
        public int HalfDays { get; set; }
        public int LeaveDays { get; set; }
        public int HolidayDays { get; set; }
        public int WeekOffDays { get; set; }

        public decimal TotalWorkingHours { get; set; }
        public decimal OvertimeHours { get; set; }

        public int RegularizationCount { get; set; }
    }

    // Org-wide KPI snapshot - api/attendanceinsights/dashboard
    public class AttendanceDashboardDto
    {
        public int PresentToday { get; set; }
        public int AbsentToday { get; set; }
        public int LateToday { get; set; }
        public int OnLeaveToday { get; set; }

        public int TotalActiveEmployees { get; set; }

        public decimal PresentPercentToday { get; set; }

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
