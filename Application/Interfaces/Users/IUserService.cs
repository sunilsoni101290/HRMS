using Application.DTOs.Auth;
using Application.DTOs.Users;

namespace Application.Interfaces.Users
{
    public interface IUserService
    {
        // actingUserId identifies who is calling, so System Configurator
        // accounts can be hidden from a non-System-Configurator caller
        // (requirement: User Management stays reachable by Admin/HR as
        // today, but System Configurator accounts must be invisible to
        // everyone else). GetAllAsync filters at the query level; GetByIdAsync/
        // UpdateAsync/DeleteAsync treat a System Configurator target as not
        // found for a non-System-Configurator caller, same as a genuinely
        // missing id - see UserService.EnsureTargetVisibleAsync.
        Task<List<UserListDto>> GetAllAsync(string actingUserId);
        Task<UserDto> GetByIdAsync(string id, string actingUserId);
        Task<string> CreateAsync(UserDto dto);
        Task<string> UpdateAsync(string id, UserDto dto, string actingUserId);
        // Returns the password now in effect (shown once by the caller), or
        // null if the user wasn't found. If newPassword is null/empty, the
        // server auto-generates a random one; otherwise the admin-supplied
        // password is used (validated for minimum length first).
        Task<string> ResetPasswordAsync(string id, string? newPassword = null);
        Task<bool> ToggleActiveAsync(string id);
        Task<bool> ToggleLockAsync(string id);
        Task<bool> DeleteAsync(string id, string actingUserId);
    }
}
