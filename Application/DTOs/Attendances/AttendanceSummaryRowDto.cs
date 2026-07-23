namespace Application.DTOs.Attendances
{
    // One row per employee for AttendanceService.GetSummaryAsync, aggregating
    // that employee's classified days (see Domain/Helper/AttendanceStatusHelper.cs)
    // for the requested month/year. Only days up to "today" are counted into
    // the buckets below (future days in the current month are skipped).
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
}
