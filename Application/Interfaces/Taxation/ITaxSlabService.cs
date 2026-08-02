using Application.DTOs.Taxation;

namespace Application.Interfaces.Taxation
{
    // Admin-only master-data CRUD for income-tax slabs - see
    // Domain/Entities/TaxSlab.cs / TaxSlabService. Plain CRUD, no
    // workflow - every action requires the corresponding permission
    // (View/Create/Edit/Delete) on AppFeatureConstants.TAX_SLAB.
    public interface ITaxSlabService
    {
        Task<List<TaxSlabDto>> GetAllAsync(string tenantId, string? financialYearId, int? regime, string actingUserId);

        Task<TaxSlabDto> GetByIdAsync(string id, string tenantId, string actingUserId);

        Task<TaxSlabDto> CreateAsync(TaxSlabDto dto, string tenantId, string actingUserId);

        Task<TaxSlabDto> UpdateAsync(string id, TaxSlabDto dto, string tenantId, string actingUserId);

        Task<bool> DeleteAsync(string id, string tenantId, string actingUserId);
    }
}
