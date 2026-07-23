using Application.DTOs.Onboarding;
using static Domain.Enums.EnumExtensions;

namespace Application.Interfaces.Onboarding
{
    public interface IOnboardingService
    {
        #region Case CRUD

        // Validates the Employee exists (and belongs to the tenant), rejects
        // if an active case (Status NotStarted/InProgress/OnHold) already
        // exists for that EmployeeId, seeds checklist items for all 5 stages
        // from active OnboardingChecklistTemplateItem rows (falling back to
        // inline defaults per stage the first time the feature is used),
        // and sets Status = InProgress / StartDate = now.
        // Throws on validation failure (mirrors
        // AttendanceRegularizationService.CreateAsync /
        // LeaveApplicationService.CreateAsync - no Result<T> wrapper in this
        // codebase, the controller catches and returns BadRequest).
        Task<OnboardingCaseDto> CreateCaseAsync(CreateOnboardingCaseDto dto, string tenantId, string actingUserId);

        Task<OnboardingCaseDto?> GetByIdAsync(string id, string tenantId);

        Task<List<OnboardingCaseDto>> GetAllAsync(string tenantId, string? status, string? departmentId, string? search);

        // Most recent OnboardingCase for this Employee, if any.
        Task<OnboardingCaseDto?> GetByEmployeeIdAsync(string employeeId, string tenantId);

        #endregion

        #region Checklist Items

        // Sets CompletedOn/CompletedBy when moving to Completed, clears them
        // when moved away from Completed. Recomputes the parent case's
        // progress afterwards - if every mandatory item across all 5 stages
        // is now Completed or NotApplicable, auto-sets the case to
        // Completed (only if it wasn't already Completed/Cancelled).
        Task<OnboardingChecklistItemDto> UpdateChecklistItemStatusAsync(string itemId, UpdateChecklistItemStatusDto dto, string actingUserId, string tenantId);

        // Manually adds an extra checklist item to a live case (on top of
        // whatever was seeded at creation time). NOTE: the task brief listed
        // this without an acting-user parameter - actingUserId was added
        // (like every other Create method in this codebase) purely to
        // populate CreatedBy/audit fields; see final report deviations.
        Task<OnboardingChecklistItemDto> AddChecklistItemAsync(string caseId, AddChecklistItemDto dto, string tenantId, string actingUserId);

        // Deliberately allows deleting ANY checklist item (mandatory or not,
        // seeded or manually added) - kept simple rather than adding a
        // "manually-added only" provenance flag; document this at the call
        // site/UI if a stricter rule is desired later.
        Task<bool> DeleteChecklistItemAsync(string itemId, string tenantId, string actingUserId);

        #endregion

        #region Case Status Transitions

        // Hold / Cancel / Reactivate (or any other manual status change).
        Task<OnboardingCaseDto> UpdateCaseStatusAsync(string caseId, OnboardingCaseStatus newStatus, string? remarks, string tenantId);

        #endregion

        #region Template Management

        Task<List<OnboardingChecklistTemplateItemDto>> GetTemplateItemsAsync(string tenantId, OnboardingStageType? stage);

        // existingId null/empty => insert, otherwise updates that row.
        Task<OnboardingChecklistTemplateItemDto> UpsertTemplateItemAsync(UpsertOnboardingTemplateItemDto dto, string? existingId, string tenantId, string actingUserId);

        Task<bool> DeleteTemplateItemAsync(string id, string tenantId);

        #endregion
    }
}
