using Application.DTOs.EmployeeLifecycle;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Interfaces.EmployeeLifecycle
{
    // Rejoining - Phase 5 (final) of the "Probation & Confirmation"
    // (Employee Lifecycle) module. NO maker-checker workflow (like
    // IEmployeeFeedbackService) - a single-step, HR-permission-gated action
    // that rehires a FORMER employee (Employee.RelievingDate != null,
    // IsDeleted == false). See Domain/Entities/RejoiningHistory.cs /
    // RejoiningService.
    public interface IRejoiningService
    {
        // Lists FORMER employees eligible to rejoin - tenant-scoped,
        // !IsDeleted && RelievingDate != null, with optional name/code
        // search. Lightweight "picker" DTO, no permission gate at this
        // layer (mirrors ProbationConfirmationService.GetDueForReviewAsync).
        Task<List<RejoiningEligibleEmployeeDto>> GetEligibleForRejoinAsync(string tenantId, string? search);

        // The single rejoin action - requires Create permission on
        // REJOINING (HR-only, not self-service). Validates the employee
        // exists, is not deleted, has actually exited (RelievingDate !=
        // null), and that NewJoiningDate is on/after the current
        // RelievingDate. Snapshots the Employee's CURRENT
        // RelievingDate/JoiningDate into a new RejoiningHistory row BEFORE
        // mutating the live Employee: RelievingDate is cleared to null and
        // JoiningDate is set to dto.NewJoiningDate. Nothing else on the
        // Employee record is touched - see RejoiningService for the full
        // out-of-scope list (IsDeleted, EmploymentType, ConfirmationDate,
        // ProbationEndDate, Department/Designation/Company/Branch/
        // ReportingManager).
        Task<RejoiningHistoryDto> RejoinAsync(RejoinEmployeeDto dto, string tenantId, string actingUserId);

        // All RejoiningHistory rows for one employee (an employee may
        // rejoin more than once over their lifetime), tenant-scoped, newest
        // first. No permission gate at this layer, mirroring
        // EmployeeTransferService.GetTransferHistoryForEmployeeAsync.
        Task<List<RejoiningHistoryDto>> GetHistoryForEmployeeAsync(string employeeId, string tenantId);

        // View-authorization: anyone holding View permission on REJOINING
        // may view any record tenant-wide (HR-internal audit record, no
        // per-record ownership/privacy restriction) - same permissive model
        // as EmployeeTransferService/ProbationConfirmationService.
        Task<RejoiningHistoryDto> GetByIdAsync(string id, string tenantId, string actingUserId);

        // All RejoiningHistory records tenant-wide, for an HR audit list,
        // with optional name/code search.
        Task<List<RejoiningHistoryDto>> GetAllAsync(string tenantId, string? search);
    }
}
