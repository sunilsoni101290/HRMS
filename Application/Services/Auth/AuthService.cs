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
                PhoneNumber = dto.PhoneNumber,
                BranchId = dto.BranchId,
                EmployeeId = dto.EmployeeId,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                CreatedBy = string.IsNullOrEmpty(dto.CreatedBy) ? "System" : dto.CreatedBy
            };

            _db.Users.Add(user);

            // Default Role Assign
            var role =  _db.Roles.FirstOrDefault(x => x.Code == ConstantHelper.EMPLOYEE);

            if (role != null)
            {
                _db.UserRoles.Add(new UserRole
                {
                    Id = IDManager.GetNewId(new UserRole()),
                    UserId = user.Id,
                    RoleId = string.IsNullOrEmpty(dto.RoleId) ? role.Id :dto.RoleId,
                    CreatedBy = string.IsNullOrEmpty(dto.CreatedBy) ? "System" : dto.CreatedBy
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
            var user = await _db.Users.Include(x => x.Employee)
                .ThenInclude(x => x.Designation)
                .Include(x => x.UserRoles)
                .ThenInclude(x => x.Role)
                .Include(x => x.Company)
                .Include(x => x.Branch)
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

            var roleName = _db.UserRoles
            .Where(x => x.UserId == user.Id)
            .Select(x => x.Role.Name)
            .FirstOrDefault();

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
                Designation = !string.IsNullOrEmpty(user.EmployeeId) ? await GetDesignationName(user.EmployeeId) : "UNKNOWN",
                RoleName = roleName,
                FullName = !string.IsNullOrEmpty(user.EmployeeId) ? await GetEmployeeFullName(user.EmployeeId) : "UNKNOWN",
                Email = user.Email,
            };
        }

        private async Task<string> GetEmployeeFullName(string employeeId)
        {
            var emp = await _db.Employees.AsNoTracking().FirstOrDefaultAsync(x=>x.Id==employeeId);

            if (emp == null) 
            {
                return "";
            }

            return emp.FirstName + " " + emp.LastName;
        }
        private async Task<string> GetDesignationName(string employeeId)
        {
            var emp = await _db.Employees.Include(x=>x.Designation).FirstOrDefaultAsync(x=>x.Id==employeeId);

            if (emp == null) 
            {
                return "";
            }

            return emp.Designation.Name;
        }

        public async Task<List<UserListDto>> GetAllAsync()
        {
            return await _db.Users
                .Include(x=>x.Employee)
                .ThenInclude(x => x.Designation)
                .Include(x => x.UserRoles)
                .ThenInclude(x => x.Role)
                .Include(x=>x.Company)
                .Include(x=>x.Branch)
                .AsNoTracking()
                .Select(x => new UserListDto
                {
                    Id = x.Id,

                    // Identity
                    Username = x.Username,
                    Email = x.Email,
                    PhoneNumber = x.PhoneNumber,

                    // Tenant
                    TenantId = x.TenantId,
                    TenantName = x.Tenant.Name,

                    // Company
                    CompanyId = x.CompanyId,
                    CompanyName = x.Company.Name,

                    // Branch
                    BranchId = x.BranchId,
                    BranchName = x.Branch != null
                        ? x.Branch.Name
                        : null,

                    // Employee
                    EmployeeId = x.EmployeeId,
                    EmployeeName = x.Employee != null
                        ? x.Employee.FirstName + " " + x.Employee.LastName
                        : null,

                    // Security
                    EmailConfirmed = x.EmailConfirmed,
                    PhoneConfirmed = x.PhoneConfirmed,
                    IsLocked = x.IsLocked,
                    AccessFailedCount = x.AccessFailedCount,

                    // Login
                    LastLoginDate = x.LastLoginDate,
                    LastLoginIP = x.LastLoginIP,

                    // Common
                    IsActive = x.IsActive,
                    CreatedDate = x.CreatedOn,
                    CreatedBy=x.CreatedBy,
                })
                .OrderByDescending(x => x.CreatedDate)
                .ToListAsync();
        }

        public async Task<UserListDto?> GetByIdAsync(string id)
        {
            return await _db.Users
                .AsNoTracking()

                // Includes
                .Include(x => x.Employee)
                .Include(x => x.Company)
                .Include(x => x.Branch)
                .Include(x => x.Tenant)
                .Include(x => x.UserRoles)
                    .ThenInclude(ur => ur.Role)

                // Filter
                .Where(x => x.Id == id)

                // Select DTO
                .Select(x => new UserListDto
                {
                    Id = x.Id,

                    // Identity
                    Username = x.Username,
                    Email = x.Email,
                    PhoneNumber = x.PhoneNumber,

                    // Tenant
                    TenantId = x.TenantId,
                    TenantName = x.Tenant != null
                        ? x.Tenant.Name
                        : null,

                    // Company
                    CompanyId = x.CompanyId,
                    CompanyName = x.Company != null
                        ? x.Company.Name
                        : null,

                    // Role (First Role)
                    RoleId = x.UserRoles
                        .Select(r => r.RoleId)
                        .FirstOrDefault(),

                    RoleName = x.UserRoles
                        .Select(r => r.Role.Name)
                        .FirstOrDefault(),

                    // Branch
                    BranchId = x.BranchId,
                    BranchName = x.Branch != null
                        ? x.Branch.Name
                        : null,

                    // Employee
                    EmployeeId = x.EmployeeId,

                    EmployeeName = x.Employee != null
                        ? (x.Employee.FirstName + " " + x.Employee.LastName)
                        : null,

                    // Security
                    EmailConfirmed = x.EmailConfirmed,
                    PhoneConfirmed = x.PhoneConfirmed,
                    IsLocked = x.IsLocked,
                    AccessFailedCount = x.AccessFailedCount,

                    // Login
                    LastLoginDate = x.LastLoginDate,
                    LastLoginIP = x.LastLoginIP,

                    // Common
                    IsActive = x.IsActive,
                    CreatedDate = x.CreatedOn,
                    CreatedBy = x.CreatedBy
                })

                .FirstOrDefaultAsync();
        }
    }
}
