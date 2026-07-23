using System;

namespace Application.DTOs.Attendances
{
    // One row per team member for AttendanceService.GetTeamAttendanceAsync,
    // for a single given date. Status uses the same classification values
    // as the ESS dashboard's Week Attendance strip - see
    // Domain/Helper/AttendanceStatusHelper.cs.
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
}
