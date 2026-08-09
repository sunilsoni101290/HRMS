using Application.DTOs.LoanAdvance;

namespace Application.Interfaces.LoanAdvance
{
    public interface IAdvanceTypeService
    {
        Task<List<AdvanceTypeDto>> GetAllAsync(string tenantId, string actingUserId, bool includeInactive = false);
        Task<AdvanceTypeDto> GetByIdAsync(string id, string tenantId, string actingUserId);
        Task<AdvanceTypeDto> CreateAsync(AdvanceTypeDto dto, string tenantId, string actingUserId);
        Task<AdvanceTypeDto> UpdateAsync(AdvanceTypeDto dto, string tenantId, string actingUserId);
        Task<bool> DeleteAsync(string id, string tenantId, string actingUserId);
    }
}
