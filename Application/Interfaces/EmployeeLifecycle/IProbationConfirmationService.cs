using Application.DTOs.EmployeeLifecycle;

namespace Application.Interfaces.EmployeeLifecycle
{
    // Foundational Maker-Checker (segregation-of-duties) service for the
    // Probation & Confirmation module - see
    // Domain/Entities/ProbationConfirmation.cs /
    // Application/Services/EmployeeLifecycle/ProbationConfirmationService.cs
    // for the actingUserId != MakerId invariant enforced inside
    // ApproveAsync/RejectAsync. Later PIP Outcome / Employee Transfer
    // agents should mirror this interface shape for their own equivalent
    // service.
    public interface IProbationConfirmationService
    {
        // Employees whose probation is due for review - EmploymentType ==
        // Probation, not exited (RelievingDate == null), and either
        // ProbationEndDate is null or within a 14-day lookahead window -
        // excludes employees who already have a PendingChecker proposal
        // open.
        Task<List<ProbationDueForReviewDto>> GetDueForReviewAsync(string tenantId, string? departmentId, string? search, string actingUserId);

        // Maker action - proposes an outcome for an employee's probation.
        // actingUserId becomes MakerId; must hold Create permission on
        // PROBATION_CONFIRMATION.
        Task<ProbationConfirmationDto> CreateAsync(CreateProbationConfirmationDto dto, string tenantId, string actingUserId);

        // View-authorization: anyone holding View permission on
        // PROBATION_CONFIRMATION may view any record tenant-wide (HR-
        // internal records, no per-record ownership/privacy restriction).
        Task<ProbationConfirmationDto> GetByIdAsync(string id, string tenantId, string actingUserId);

        Task<List<ProbationConfirmationDto>> GetAllAsync(string tenantId, string? status, string? departmentId, string? search, string actingUserId);

        // Checker action - Approve. THE CORE INVARIANT: actingUserId must
        // differ from the record's MakerId (checked before the permission
        // check, no override). Applies the outcome to the Employee record
        // per Recommendation - see ProbationConfirmationService for the
        // exact per-outcome behavior.
        Task<ProbationConfirmationDto> ApproveAsync(string id, CheckerActionDto dto, string actingUserId, string tenantId);

        // Checker action - Reject. Same actingUserId != MakerId invariant
        // as ApproveAsync; CheckerRemarks required. No Employee changes are
        // applied.
        Task<ProbationConfirmationDto> RejectAsync(string id, CheckerActionDto dto, string actingUserId, string tenantId);
    }
}
