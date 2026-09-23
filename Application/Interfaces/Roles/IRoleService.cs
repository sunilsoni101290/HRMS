using Application.DTOs.Roles;

namespace Application.Interfaces.Roles
{
    public interface IRoleService
    {
        // Role Management CRUD - System Configurator ONLY (see
        // EnsurePermissionAsync in RoleService). actingUserId is always
        // passed explicitly rather than trusted implicitly, same
        // convention as IErrorLogService/IDatabaseManagementService.
        // AssignPermissionsAsync reuses request.ModifiedBy as the acting
        // user - no separate parameter needed.
        Task<List<RoleListDto>> GetAllAsync(string actingUserId);
        Task<RoleDto> GetByIdAsync(string id, string actingUserId);
        Task<RoleDetailDto> GetDetailAsync(string id, string actingUserId);
        Task<string> CreateAsync(RoleDto dto, string actingUserId);
        Task<string> UpdateAsync(string id, RoleDto dto, string actingUserId);
        Task<bool> ToggleActiveAsync(string id, string actingUserId);
        Task<bool> DeleteAsync(string id, string actingUserId);
        Task<bool> AssignPermissionsAsync(AssignRolePermissionsRequestDto request);
    }
}
