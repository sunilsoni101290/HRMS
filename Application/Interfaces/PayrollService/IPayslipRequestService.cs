using Application.DTOs.Payroll;

namespace Application.Interfaces.Payroll
{
    // Employee -> Reporting Manager -> Finance payslip request workflow -
    // see Domain/Entities/PayslipRequest.cs for the full state machine.
    // Every method resolves the acting User.Id's authorization internally
    // (never trusts a client-supplied EmployeeId) - same shape as
    // IWfhRequestService.
    public interface IPayslipRequestService
    {
        Task<PayslipRequestDto> CreateAsync(CreatePayslipRequestDto dto, string tenantId, string actingUserId);

        // Visible to the request's own employee, their direct Reporting
        // Manager, or Finance/HR/Admin override.
        Task<PayslipRequestDto> GetByIdAsync(string id, string tenantId, string actingUserId);

        Task<List<PayslipRequestDto>> GetMyRequestsAsync(string actingUserId, string tenantId);

        // Requests currently PendingManagerApproval where the acting user is
        // the requesting employee's direct Reporting Manager (org-wide if
        // Finance/HR/Admin override).
        Task<List<PayslipRequestDto>> GetPendingForManagerAsync(string actingUserId, string tenantId);

        // Approves at the manager stage AND, in the same call, immediately
        // forwards to Finance (PendingManagerApproval -> ApprovedByManager
        // -> PendingFinanceAction) - there is no separate manual "forward"
        // step. Logs both transitions and notifies Finance at the end.
        Task<PayslipRequestDto> ManagerApproveAsync(string id, string? remarks, string actingUserId, string tenantId);

        Task<PayslipRequestDto> ManagerRejectAsync(string id, string reason, string actingUserId, string tenantId);

        // Finance's queue - both PendingFinanceAction (needs upload) and
        // PayslipGenerated (uploaded, needs Complete) so Finance has one
        // queue for both steps. Requires the Finance/HR/Admin permission
        // override - returns an empty list rather than throwing otherwise.
        Task<List<PayslipRequestDto>> GetPendingForFinanceAsync(string actingUserId, string tenantId);

        Task<PayslipRequestDto> FinanceUploadAsync(string id, string documentUrl, string documentFileName, string? remarks, string actingUserId, string tenantId);

        Task<PayslipRequestDto> FinanceCompleteAsync(string id, string? remarks, string actingUserId, string tenantId);

        Task<PayslipRequestDto> FinanceRejectAsync(string id, string reason, string actingUserId, string tenantId);

        // Only ever succeeds when Status == Completed and the caller is the
        // request's own employee (or Finance/HR/Admin override). Logs a
        // "Downloaded" audit row. The caller (APP controller) uses the
        // returned DocumentUrl to stream/redirect to the actual file - an
        // employee can never reach the file any other way.
        Task<PayslipRequestDto> GetForDownloadAsync(string id, string actingUserId, string tenantId);

        Task<List<PayslipRequestDto>> GetAllAsync(string tenantId, string? status);
    }
}
