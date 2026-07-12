using Application.DTOs.Permissions;

namespace Application.Interfaces.Permissions
{
    public interface IPermissionService
    {
        Task<List<PermissionListDto>> GetAllAsync();
        Task<PermissionDto> GetByIdAsync(string id);
        Task<string> CreateAsync(PermissionDto dto);
        Task<string> UpdateAsync(string id, PermissionDto dto);
        Task<bool> DeleteAsync(string id);
    }
}
