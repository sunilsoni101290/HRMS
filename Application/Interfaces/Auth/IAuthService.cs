using Application.DTOs.Auth;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces.Auth
{
    public interface IAuthService
    {
        Task<AuthResponse> RegisterAsync(RegisterDto dto);
        Task<User> GetUserDetailByIdAsync(string id); // Only for update scenario
        Task<User> GettUserDetailByUsernameAsync(string username);
        Task<bool> UpdateUserAsync(User user);
        Task<AuthResponse> LoginAsync(LoginDto dto);
        Task<AuthResponse> RefreshTokenAsync(string refreshToken);
        Task<bool> LogoutAsync(string refreshToken);
        Task<List<UserListDto>> GetAllAsync();
        Task<UserListDto?> GetByIdAsync(string id);
        Task<UserListDto?> GetUserDetailsByEmpIdAsync(string empId);
        Task<ChangePasswordResultDto> ChangePasswordAsync(string userId, string oldPassword, string newPassword);
        Task<ForgotPasswordResultDto> ForgotPasswordAsync(string username, string email, string newPassword);
    }
}
