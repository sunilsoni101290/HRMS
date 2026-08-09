using Application.DTOs.LoanAdvance;

namespace Application.Interfaces.LoanAdvance
{
    public interface ILoanTypeService
    {
        Task<List<LoanTypeDto>> GetAllAsync(string tenantId, string actingUserId, bool includeInactive = false);
        Task<LoanTypeDto> GetByIdAsync(string id, string tenantId, string actingUserId);
        Task<LoanTypeDto> CreateAsync(LoanTypeDto dto, string tenantId, string actingUserId);
        Task<LoanTypeDto> UpdateAsync(LoanTypeDto dto, string tenantId, string actingUserId);

        /// <summary>Soft delete - blocked when any EmployeeLoan still references this type (see ActiveLoanCount).</summary>
        Task<bool> DeleteAsync(string id, string tenantId, string actingUserId);
    }
}
