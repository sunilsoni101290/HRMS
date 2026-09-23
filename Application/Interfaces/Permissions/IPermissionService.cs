using Application.DTOs.Permissions;

namespace Application.Interfaces.Permissions
{
    public interface IPermissionService
    {
        // Permission Management CRUD - System Configurator ONLY (see
        // EnsurePermissionAsync in PermissionService). actingUserId is
        // always passed explicitly rather than trusted implicitly, same
        // convention as IErrorLogService/IDatabaseManagementService.
        Task<List<PermissionListDto>> GetAllAsync(string actingUserId);
        Task<PermissionDto> GetByIdAsync(string id, string actingUserId);
        Task<string> CreateAsync(PermissionDto dto, string actingUserId);
        Task<string> UpdateAsync(string id, PermissionDto dto, string actingUserId);
        Task<bool> DeleteAsync(string id, string actingUserId);
    }
}
