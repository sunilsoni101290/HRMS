using Application.DTOs.Attendances;

namespace Application.Interfaces.Attendances
{
    public interface IAttendancePolicyService
    {
        Task<List<AttendancePolicyDto>> GetAllAsync(string tenantId);

        Task<AttendancePolicyDto?> GetByIdAsync(string id, string tenantId);

        // Currently-effective policy: IsActive=true, most recent
        // EffectiveFrom <= today, matching CompanyId if provided, else the
        // tenant-wide (CompanyId == null) fallback policy.
        Task<AttendancePolicyDto?> GetActiveForTenantAsync(string tenantId, string? companyId);

        Task<AttendancePolicyDto> CreateAsync(AttendancePolicyDto dto, string tenantId, string? actingUserId);

        Task<AttendancePolicyDto> UpdateAsync(string id, AttendancePolicyDto dto, string tenantId, string? actingUserId);

        Task<bool> DeleteAsync(string id, string tenantId);
    }
}
