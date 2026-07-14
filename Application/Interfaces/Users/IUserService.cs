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
        // Returns the password now in effect (shown once by the caller), or
        // null if the user wasn't found. If newPassword is null/empty, the
        // server auto-generates a random one; otherwise the admin-supplied
        // password is used (validated for minimum length first).
        Task<string> ResetPasswordAsync(string id, string? newPassword = null);
        Task<bool> ToggleActiveAsync(string id);
        Task<bool> ToggleLockAsync(string id);
        Task<bool> DeleteAsync(string id);
    }
}
