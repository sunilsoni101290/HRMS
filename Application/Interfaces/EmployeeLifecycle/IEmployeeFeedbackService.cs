using Application.DTOs.EmployeeLifecycle;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Interfaces.EmployeeLifecycle
{
    // Employee Feedback - Phase 4 of the "Probation & Confirmation"
    // (Employee Lifecycle) module. Plain CRUD, NO maker-checker workflow
    // (unlike IProbationConfirmationService/IPipService/
    // IEmployeeTransferService) - see Domain/Entities/EmployeeFeedback.cs /
    // EmployeeFeedbackService for the Reporting-Manager-or-HR authorization
    // rules and the IsVisibleToEmployee-driven visibility filter.
    public interface IEmployeeFeedbackService
    {
        // Create authorization: the acting user must be the target
        // Employee's current ReportingManagerId, OR hold Create permission
        // on EMPLOYEE_FEEDBACK (HR/Admin). Sets GivenByUserId = actingUserId.
        Task<EmployeeFeedbackDto> CreateAsync(CreateUpdateEmployeeFeedbackDto dto, string tenantId, string actingUserId);

        // Update authorization: only the original author (GivenByUserId ==
        // actingUserId) or someone holding Edit permission (HR/Admin).
        Task<EmployeeFeedbackDto> UpdateAsync(string id, CreateUpdateEmployeeFeedbackDto dto, string tenantId, string actingUserId);

        // Delete authorization: same as Update (author or Edit-permission
        // holder). Soft delete via BaseEntity.IsDeleted.
        Task DeleteAsync(string id, string tenantId, string actingUserId);

        // View authorization: the subject Employee themself (only if
        // IsVisibleToEmployee == true AND the acting user's own linked
        // Employee record IS the subject), the author, the subject's
        // current Reporting Manager, or anyone holding View permission
        // (HR/Admin). If IsVisibleToEmployee == false, the subject Employee
        // themself cannot view it - enforced exactly, that's the point of
        // the flag.
        Task<EmployeeFeedbackDto> GetByIdAsync(string id, string tenantId, string actingUserId);

        // Primary listing method - all feedback for one employee, with the
        // SAME per-record visibility filter as GetByIdAsync applied: if the
        // caller is the subject employee, only rows with
        // IsVisibleToEmployee == true are returned; if the caller is the
        // author, the subject's Reporting Manager, or holds View permission
        // (HR/Admin), all rows are returned. Both the self-service "my
        // feedback" view and the HR/manager "feedback about employee X"
        // view go through this one method with different callers.
        Task<List<EmployeeFeedbackDto>> GetForEmployeeAsync(string employeeId, string tenantId, string actingUserId);

        // Convenience listing - all feedback where GivenByUserId ==
        // actingUserId, for a manager's "feedback I've given" view.
        // Inherently scoped to the caller's own submissions, no extra
        // authorization needed.
        Task<List<EmployeeFeedbackDto>> GetGivenByMeAsync(string tenantId, string actingUserId);
    }
}
