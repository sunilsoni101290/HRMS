using Application.DTOs.LoanAdvance;

namespace Application.Interfaces.LoanAdvance
{
    public interface ILoanPolicyService
    {
        Task<List<LoanPolicyDto>> GetAllAsync(string tenantId, string actingUserId, string? loanTypeId = null, string? companyId = null);
        Task<LoanPolicyDto> GetByIdAsync(string id, string tenantId, string actingUserId);

        /// <summary>Creates VersionNumber=1 with its approval matrix.</summary>
        Task<LoanPolicyDto> CreateAsync(LoanPolicyDto dto, string tenantId, string actingUserId);

        /// <summary>
        /// Does NOT mutate the existing row - closes it (EffectiveTo = now,
        /// IsActive = false) and inserts a new row with
        /// VersionNumber + 1, so EmployeeLoan.LoanPolicyId snapshots taken
        /// against the old version keep their original terms. See
        /// Domain/Entities/LoanPolicy.cs.
        /// </summary>
        Task<LoanPolicyDto> UpdateAsync(LoanPolicyDto dto, string tenantId, string actingUserId);

        Task<bool> DeactivateAsync(string id, string tenantId, string actingUserId);

        /// <summary>Resolves the single currently-active policy for a LoanType/Company/Branch combination (most specific match wins).</summary>
        Task<LoanPolicyDto?> GetActivePolicyAsync(string loanTypeId, string tenantId, string? companyId, string? branchId);
    }
}
