using Application.DTOs.Roles;

namespace Application.Interfaces.Roles
{
    public interface IRoleService
    {
        Task<List<RoleListDto>> GetAllAsync();
        Task<RoleDto> GetByIdAsync(string id);
        Task<RoleDetailDto> GetDetailAsync(string id);
        Task<string> CreateAsync(RoleDto dto);
        Task<string> UpdateAsync(string id, RoleDto dto);
        Task<bool> ToggleActiveAsync(string id);
        Task<bool> DeleteAsync(string id);
        Task<bool> AssignPermissionsAsync(AssignRolePermissionsRequestDto request);
    }
}
