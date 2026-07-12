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
        // Returns the newly generated temporary password (shown once by the
        // caller), or null if the user wasn't found. Never accepts a
        // caller-supplied password - the server always generates it.
        Task<string> ResetPasswordAsync(string id);
        Task<bool> ToggleActiveAsync(string id);
        Task<bool> ToggleLockAsync(string id);
        Task<bool> DeleteAsync(string id);
    }
}
