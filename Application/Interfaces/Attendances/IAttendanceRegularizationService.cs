using Application.DTOs.Attendances;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces.Attendances
{
    public interface IAttendanceRegularizationService
    {
        #region CRUD
        Task<AttendanceRegularizationDto> CreateAsync(RequestRegularizationDto request);
        Task<AttendanceRegularizationDto> GetByIdAsync(string id);
        Task<List<AttendanceRegularizationDto>> GetAllAsync();
        #endregion

        #region Workflow
        Task<bool> RequestRegularizationAsync(RequestRegularizationDto request);
        Task<bool> ApproveAsync(ApproveRegularizationRequestDto request);
        Task<bool> RejectAsync(RejectRegularizationRequestDto request);
        Task<bool> SendBackAsync(SendBackRegularizationRequestDto request);
        Task<AttendanceRegularizationDto> ResubmitAsync(string id, RequestRegularizationDto request, string resubmittedBy);
        Task<bool> CancelAsync(CancelRegularizationRequestDto request);
        #endregion

        #region Queries
        Task<List<AttendanceRegularizationDto>> GetEmployeeRequestsAsync(string employeeId);
        Task<List<AttendanceRegularizationDto>> GetPendingAsync();

        // Regularization requests currently awaiting action from this
        // specific approver - Level 1/2 requests where they are the
        // resolved Reporting Manager/Department Head (or an active delegate),
        // plus every Level 3 request if they hold the Approve permission on
        // the Attendance Regularization feature (see IsHrApproverAsync).
        Task<List<AttendanceRegularizationDto>> GetPendingForApproverAsync(string? employeeId, string? userId);

        // Cosmetic-only helper for the APP self-service UI (button
        // visibility) - the real Level 3 enforcement always happens inside
        // ApproveAsync/RejectAsync/SendBackAsync via IsAuthorizedForLevelAsync
        // regardless of what this returns.
        Task<bool> IsHrApproverAsync(string? actingUserId);
        Task<List<AttendanceRegularizationDto>> GetApprovedAsync();
        Task<List<AttendanceRegularizationDto>> GetRejectedAsync();
        Task<List<AttendanceRegularizationDto>> GetCancelledAsync();
        Task<List<AttendanceRegularizationDto>> GetFilteredAsync(AttendanceRegularizationFilterRequestDto request);
        #endregion

        #region Approval History
        Task<List<AttendanceRegularizationApprovalHistoryDetailDto>> GetApprovalHistoryAsync(string attendanceRegularizationId);
        Task<List<AttendanceRegularizationApprovalHistoryDetailDto>> GetAllApprovalHistoryAsync();
        Task<AttendanceRegularizationApprovalHistoryDetailDto> GetApprovalHistoryByIdAsync(string id);
        #endregion
    }
}
