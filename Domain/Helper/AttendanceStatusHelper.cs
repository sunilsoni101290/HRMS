using System;
using static Domain.Enums.EnumExtensions;

namespace Domain.Helper
{
    // Shared per-day attendance status classification used by both:
    //  - the legacy Employee Self-Service "Week Attendance" strip
    //    (Application/Services/Dashboard/EmployeeDashboardService.cs), and
    //  - the new Attendance Calendar/Team/Summary/Dashboard aggregation
    //    endpoints (Application/Services/Attendances/AttendanceService.cs -
    //    GetCalendarAsync/GetTeamAttendanceAsync/GetSummaryAsync/GetDashboardAsync).
    //
    // MapLegacyWeekStatus() is an exact, behavior-preserving extraction of
    // the ESS dashboard's original inline logic
    // (`rec != null ? rec.Status.ToString() : "None"`) - it deliberately
    // does NOT cross-reference Leave/Holiday/WeekOff, so the ESS widget's
    // existing output is unchanged by this refactor.
    //
    // ClassifyDay() is the richer version used by the new endpoints - it
    // resolves a day with no Attendance row (or an Attendance row still at
    // AttendanceStatus.None) against approved Leave / Holiday / WeekOff
    // data before falling back to Absent (past dates) or None (future
    // dates).
    //
    // Consistency guarantee: whenever an Attendance row with a non-None
    // Status exists for the day, BOTH methods return the exact same string
    // (status.ToString()) - they only diverge on days with no explicit
    // Attendance status, where the legacy method has always shown "None"
    // and the new one now resolves the real reason. So a day that the ESS
    // strip shows as "Present" (or any other explicit status) can never be
    // shown as a contradicting status (e.g. "Absent") by the new
    // Calendar/Team/Summary/Dashboard endpoints for that same employee/date.
    public static class AttendanceStatusHelper
    {
        public static string MapLegacyWeekStatus(AttendanceStatus? status)
        {
            return status.HasValue ? status.Value.ToString() : "None";
        }

        public static string ClassifyDay(
            AttendanceStatus? attendanceStatus,
            bool hasApprovedLeave,
            bool isHoliday,
            bool isWeekOff,
            DateTime date,
            DateTime today)
        {
            // An explicit, non-None Attendance.Status always wins - this is
            // what keeps this method consistent with the legacy mapping
            // above and with the Attendance master CRUD screen (which lets
            // HR set Status to any AttendanceStatus value directly, e.g.
            // WorkFromHome/OnDuty/Overtime/CompOff/Late).
            if (attendanceStatus.HasValue && attendanceStatus.Value != AttendanceStatus.None)
                return attendanceStatus.Value.ToString();

            if (hasApprovedLeave)
                return "Leave";

            if (isHoliday)
                return "Holiday";

            if (isWeekOff)
                return "WeekOff";

            // No Attendance row and no Leave/Holiday/WeekOff explanation -
            // a future date simply has no status yet, a past/today date
            // with no punch is Absent.
            return date.Date > today.Date ? "None" : "Absent";
        }
    }
}
