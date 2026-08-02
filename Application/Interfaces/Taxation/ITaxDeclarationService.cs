using Application.DTOs.Taxation;

namespace Application.Interfaces.Taxation
{
    // Employee self-service investment declaration - see
    // Domain/Entities/TaxDeclaration.cs / TaxDeclarationService.
    // Draft -> Submitted (employee) -> Verified/Rejected (HR/Payroll,
    // Approve permission on TAX_DECLARATION). NOT a Maker-Checker
    // (actingUserId != MakerId) feature - see TaxDeclarationService's
    // header comment for why.
    public interface ITaxDeclarationService
    {
        // Employee self-service: fetch the acting user's own (Employee-
        // resolved) declaration for a Financial Year, or null if none
        // exists yet.
        Task<TaxDeclarationDto?> GetMyDeclarationAsync(string financialYearId, string tenantId, string actingUserId);

        // Creates a new Draft, or updates the existing Draft in place if
        // one already exists for (EmployeeId, FinancialYearId) - upsert,
        // matching how an annual declaration is naturally edited multiple
        // times before submission. Only the declaration's own employee,
        // or HR/Admin acting on their behalf (Create permission on
        // TAX_DECLARATION), may call this - never trusts dto.EmployeeId
        // alone, same pattern as WfhRequestService.CreateAsync.
        Task<TaxDeclarationDto> CreateOrUpdateAsync(CreateTaxDeclarationDto dto, string tenantId, string actingUserId);

        // Employee action - Draft -> Submitted. Locks the declaration from
        // further employee edits until HR Rejects it (which reopens it
        // for editing).
        Task<TaxDeclarationDto> SubmitAsync(string id, string tenantId, string actingUserId);

        Task<TaxDeclarationDto> GetByIdAsync(string id, string tenantId, string actingUserId);

        // HR/Payroll view - all declarations tenant-wide, optionally
        // filtered.
        Task<List<TaxDeclarationDto>> GetAllAsync(string tenantId, string? financialYearId, string? status, string? departmentId, string? search, string actingUserId);

        // HR action - Submitted -> Verified. Triggers
        // ITaxComputationService.ComputeAsync for this employee/FY so the
        // computed numbers are immediately current.
        Task<TaxDeclarationDto> VerifyAsync(string id, TaxDeclarationVerifyActionDto dto, string tenantId, string actingUserId);

        // HR action - Submitted -> Rejected. VerifierRemarks required.
        // Reopens the declaration for employee editing (a Rejected
        // declaration can be edited via CreateOrUpdateAsync again, which
        // resets Status back to Draft).
        Task<TaxDeclarationDto> RejectAsync(string id, TaxDeclarationVerifyActionDto dto, string tenantId, string actingUserId);
    }
}
