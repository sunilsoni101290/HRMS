using Application.DTOs.Auth;
using Application.DTOs.Users;

namespace Application.Interfaces.Users
{
    public interface IUserService
    {
        Task<List<UserListDto>> GetAllAsync();
        Task<UserDto> GetByIdAsync(string id);
        Task<string> CreateAsync(UserDto dto);
        Task<string> UpdateAsync(string id, UserDto dto);
        Task<bool> ResetPasswordAsync(string id, string newPassword);
        Task<bool> ToggleActiveAsync(string id);
        Task<bool> ToggleLockAsync(string id);
        Task<bool> DeleteAsync(string id);
    }
}
