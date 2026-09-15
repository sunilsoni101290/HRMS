using Application.DTOs.Attendance;
using Application.DTOs.Attendances;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces.Attendances
{
    public interface IAttendanceService
    {
        #region Attendance logs
        Task<bool> PunchInAsync(PunchRequestDto dto);
        Task<bool> PunchOutAsync(PunchRequestDto dto);
        Task<bool> BreakInAsync(PunchRequestDto dto);
        Task<bool> BreakOutAsync(PunchRequestDto dto);

        // "WithReason" variants of the four methods above - same exact
        // business logic (each public method above just delegates to the
        // same private *CoreAsync as these), but instead of swallowing the
        // exception into a bare `false`, they surface the exact reason
        // ("Already punched in", "Shift not assigned", "Punch in not
        // found", a DB error message, etc.) and, for PunchIn, whether a
        // brand-new Attendance row was created vs an existing one reused.
        // Added for AttendanceProcessorService's biometric batch processing
        // (see requirement: per-row diagnostics/reconciliation must show
        // the EXACT reason a raw punch did not become an AttendanceLog) -
        // the plain bool-returning methods above are unchanged and every
        // other existing caller (AttendanceController's live self-service
        // punch endpoints, etc.) keeps working exactly as before.
        Task<(bool Success, string? Reason, bool AttendanceCreated)> PunchInWithReasonAsync(PunchRequestDto dto);
        Task<(bool Success, string? Reason, bool AttendanceCreated)> PunchOutWithReasonAsync(PunchRequestDto dto);
        Task<(bool Success, string? Reason, bool AttendanceCreated)> BreakInWithReasonAsync(PunchRequestDto dto);
        Task<(bool Success, string? Reason, bool AttendanceCreated)> BreakOutWithReasonAsync(PunchRequestDto dto);
        Task<AttendanceCurrentStatusDto>GetCurrentStatusAsync(string employeeId);
        Task ProcessMonthlyAttendance(int year, int month);
        Task<List<Attendance>> GetMonthlyAsync(string employeeId, int month, int year);
        Task<object> GetLiveStatus(string employeeId);
        Task<List<AttendanceLogDto>> GetAllAsync();
        Task<AttendanceLogDto?> GetByIdAsync(string id);
        #endregion

        #region Attendance
        Task<List<AttendanceDto>> GetAllAttendanceListAsync();

        Task<AttendanceDto?> GetAttendanceByIdAsync(string id);

        Task<bool> CreateAsync(AttendanceDto dto);

        Task<bool> UpdateAsync(AttendanceDto dto);

        Task<bool> DeleteAsync(string id);
        #endregion

        #region Attendance Insights (Calendar / Team / Summary / Dashboard)

        // Month-grid classification for one employee - see
        // Domain/Helper/AttendanceStatusHelper.cs for the status values.
        Task<List<AttendanceCalendarDayDto>> GetCalendarAsync(string employeeId, int month, int year, string tenantId);

        // Does the acting user (by User.Id) hold an allowed RolePermission
        // for the View action on the ATTENDANCE feature - mirrors
        // AttendanceRegularizationService.IsHrApproverAsync's exact join
        // pattern, swapped to ATTENDANCE/View instead of
        // ATTENDANCE_REGULARIZATION/Approve.
        Task<bool> IsHrOrAdminForAttendanceAsync(string? actingUserId);

        // isHrOrAdmin=true -> every employee in the tenant for that date;
        // otherwise only employees reporting to the acting user's linked
        // Employee (Employee.ReportingManagerId).
        Task<List<TeamAttendanceMemberDto>> GetTeamAttendanceAsync(string actingUserId, DateTime date, string tenantId, bool isHrOrAdmin);

        Task<List<AttendanceSummaryRowDto>> GetSummaryAsync(string tenantId, int month, int year, string? departmentId, string? employeeId);

        Task<AttendanceDashboardDto> GetDashboardAsync(string tenantId, string? companyId);

        #endregion
    }
}
