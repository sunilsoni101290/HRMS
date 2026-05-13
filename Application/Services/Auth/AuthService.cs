using Application.DTOs.Auth;
using Application.Interfaces.Auth;
using Application.Interfaces.JWT_TOKEN;
using Domain.Entities;
using Domain.Helper;
using Infrastructure;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Services.Auth
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _db;
        private readonly IJwtService _jwt;
        private readonly IConfiguration _config;

        public AuthService(ApplicationDbContext db, IJwtService jwt, IConfiguration config)
        {
            _db = db;
            _jwt = jwt;
            _config= config;
        }

        public async Task<AuthResponse> RegisterAsync(RegisterDto dto)
        {
            if (_db.Users.Any(x => x.Username == dto.Username))
                throw new Exception("User already exists");

            var user = new User
            {
                Id = IDManager.GetNewId(new User()),
                Username = dto.Username,
                Email = dto.Email,
                TenantId = dto.TenantId,
                CompanyId = dto.CompanyId,
                PhoneNumber= dto.PhoneNumber,
                BranchId = dto.BranchId,
                EmployeeId = dto.EmployeeId,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                CreatedBy="system"
            };

            _db.Users.Add(user);

            // Default Role Assign
            var role = _db.Roles.FirstOrDefault(x => x.Code == ConstantHelper.EMPLOYEE);

            if (role != null)
            {
                _db.UserRoles.Add(new UserRole
                {
                    Id = IDManager.GetNewId(new UserRole()),
                    UserId = user.Id,
                    RoleId = role.Id,
                    CreatedBy = "system"
                });
            }

            await _db.SaveChangesAsync();

            return await GenerateAuthResponse(user);
        }


        // ==============================
        // 🔐 LOGIN
        // ==============================
        public async Task<AuthResponse> LoginAsync(LoginDto dto)
        {
            var user = await _db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.Username == dto.Username);

            if (user == null)
                throw new Exception("Invalid username");

            bool isValidPassword =
                BCrypt.Net.BCrypt.Verify(
                    dto.Password,
                    user.PasswordHash);

            if (!isValidPassword)
                throw new Exception("Invalid password");

            return await GenerateAuthResponse(user);
        }

        // ==============================
        // 🔄 REFRESH TOKEN
        // ==============================
        public async Task<AuthResponse> RefreshTokenAsync(string refreshToken)
        {
            var token = _db.RefreshTokens
                .Include(x => x.User)
                .FirstOrDefault(x => x.Token == refreshToken && !x.IsRevoked);

            if (token == null || token.ExpiryDate < DateTime.UtcNow)
                throw new Exception("Invalid refresh token");

            return await GenerateAuthResponse(token.User, refreshToken);
        }

        // ==============================
        // 🚪 LOGOUT
        // ==============================
        public async Task<bool> LogoutAsync(string refreshToken)
        {
            var token = _db.RefreshTokens
                .FirstOrDefault(x => x.Token == refreshToken);

            if (token == null)
                return false;

            token.IsRevoked = true;
            await _db.SaveChangesAsync();

            return true;
        }

        // ==============================
        // 🔥 COMMON METHOD
        // ==============================
        private async Task<AuthResponse> GenerateAuthResponse(User user, string existingRefreshToken = null)
        {
            // Roles
            var roles = _db.UserRoles
                .Where(x => x.UserId == user.Id)
                .Select(x => x.Role.Name)
                .ToList();

            // Permissions
            var permissions = _db.RolePermissions
                .Where(rp => roles.Contains(rp.Role.Name))
                .Select(rp => rp.Permission.Code)
                .ToList();

            // Access Token
            var accessToken = _jwt.GenerateAccessToken(user, roles, permissions);

            // Refresh Token
            string refreshToken = existingRefreshToken;

            if (string.IsNullOrEmpty(refreshToken))
            {
                refreshToken = _jwt.GenerateRefreshToken();

                _db.RefreshTokens.Add(new RefreshToken
                {
                    Id = IDManager.GetNewId(new RefreshToken()),
                    UserId = user.Id,
                    Token = refreshToken,
                    ExpiryDate = DateTime.UtcNow.AddDays(7),
                    CreatedBy="System",
                    TenantId=user.TenantId,
                    IsRevoked = false
                });

                await _db.SaveChangesAsync();
            }

            return new AuthResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresIn = Convert.ToInt32(_config["Jwt:ExpiryMinutes"]),
                UserId = user.Id,
                TenantId = user.TenantId,
                Username = user.Username,
                FullName = !string.IsNullOrEmpty(user.EmployeeId) ? await GetEmployeeFullName(user.EmployeeId):"" ,
                Email = user.Email,
            };
        }

        private async Task<string> GetEmployeeFullName(string employeeId)
        {
            var emp = await _db.Employees.FirstOrDefaultAsync(x=>x.Id==employeeId);

            if (emp == null) 
            {
                return "";
            }

            return emp.FirstName + " " + emp.LastName;
        }
    }
}
