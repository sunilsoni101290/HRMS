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
