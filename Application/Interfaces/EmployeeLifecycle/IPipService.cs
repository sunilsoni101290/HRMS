using Application.DTOs.EmployeeLifecycle;

namespace Application.Interfaces.EmployeeLifecycle
{
    // Phase 2 of the "Probation & Confirmation" module - Performance
    // Improvement Plan (PIP). See Domain/Entities/PipRecord.cs /
    // Application/Services/EmployeeLifecycle/PipService.cs. Unlike Phase
    // 1's ProbationConfirmation, the maker-checker gate here applies to
    // the FINAL OUTCOME RESOLUTION (ProposeOutcomeAsync/
    // ApproveOutcomeAsync/RejectOutcomeAsync), not to creation
    // (CreateAsync is an automatic system/HR hand-off with no maker-
    // checker gate).
    public interface IPipService
    {
        // HR/system creation - hands off from an Approved
        // ProbationConfirmation row with Recommendation == PlaceOnPIP.
        // Requires Create permission on PIP; NO maker-checker gate.
        Task<PipRecordDto> CreateAsync(CreatePipRecordDto dto, string tenantId, string actingUserId);

        // HR "active PIPs" tracking view - FinalOutcome == InProgress only.
        Task<List<PipRecordDto>> GetActivePipsAsync(string tenantId, string? departmentId, string? search, string actingUserId);

        // View-authorization: anyone holding View permission on PIP may
        // view any record tenant-wide (HR-internal, no per-record
        // ownership restriction) - same permissive model as
        // ProbationConfirmationService.
        Task<PipRecordDto> GetByIdAsync(string id, string tenantId, string actingUserId);

        Task<List<PipRecordDto>> GetAllAsync(string tenantId, string? finalOutcome, string? departmentId, string? search, string actingUserId);

        // Maker action - proposes the final outcome (Successful/
        // Unsuccessful). actingUserId becomes MakerId; must hold Create
        // permission on PIP. Stages the value into ProposedFinalOutcome;
        // the live FinalOutcome stays InProgress until Approved.
        Task<PipRecordDto> ProposeOutcomeAsync(string id, ProposePipOutcomeDto dto, string tenantId, string actingUserId);

        // Checker action - Approve. THE CORE INVARIANT: actingUserId must
        // differ from the record's MakerId (checked before the permission
        // check, no override). Applies ProposedFinalOutcome to the live
        // FinalOutcome and the corresponding Employee side-effect - see
        // PipService for the exact per-outcome behavior.
        Task<PipRecordDto> ApproveOutcomeAsync(string id, CheckerActionDto dto, string actingUserId, string tenantId);

        // Checker action - Reject. Same actingUserId != MakerId invariant
        // as ApproveOutcomeAsync; CheckerRemarks required. Clears
        // ProposedFinalOutcome back to null; FinalOutcome stays
        // InProgress so the maker may propose again later.
        Task<PipRecordDto> RejectOutcomeAsync(string id, CheckerActionDto dto, string actingUserId, string tenantId);
    }
}
