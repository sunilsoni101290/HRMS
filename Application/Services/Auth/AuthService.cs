using Application.DTOs.Auth;
using Application.DTOs.LoginHistory;
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
using System.Net;
using System.Text;
using static Domain.Enums.EnumExtensions;

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

        // How long a refresh token stays valid without being used at all.
        // Defaults to 30 days if not configured - matches the "stay logged
        // in for a long time" requirement while still eventually expiring
        // a truly-abandoned session rather than never expiring at all.
        private int RefreshTokenExpiryDays =>
            int.TryParse(_config["Jwt:RefreshTokenExpiryDays"], out var days) && days > 0
                ? days
                : 30;

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
            var role = _db.Roles.FirstOrDefault(x => x.Code == ConstantHelper.EMPLOYEE);

            if (role != null)
            {
                _db.UserRoles.Add(new UserRole
                {
                    Id = IDManager.GetNewId(new UserRole()),
                    UserId = user.Id,
                    RoleId = string.IsNullOrEmpty(dto.RoleId) ? role.Id : dto.RoleId,
                    CreatedBy = string.IsNullOrEmpty(dto.CreatedBy) ? "System" : dto.CreatedBy
                });
            }

            await _db.SaveChangesAsync();

            return await GenerateAuthResponse(user);
        }


        // ==============================
        // 🔐 LOGIN
        // ==============================
        public async Task<ApiResponse<AuthResponse>> LoginAsync(LoginDto dto)
        {
            var response = new ApiResponse<AuthResponse>();

            try
            {
                var user = await _db.Users
                    .Include(x => x.Employee)
                        .ThenInclude(x => x.Designation)
                    .Include(x => x.UserRoles)
                        .ThenInclude(x => x.Role)
                    .Include(x => x.Company)
                    .Include(x => x.Branch)
                    .FirstOrDefaultAsync(x => x.Username == dto.Username);

                if (user == null)
                {
                    response.Success = false;
                    response.Message = "Invalid username.";
                    return response;
                }

                // User inactive
                if (!user.IsActive)
                {
                    await RecordLoginHistoryAsync(new LoginHistoryListDto
                    {
                        TenantId = user.TenantId,
                        UserId = user.Id,
                        LoginStatus = LoginStatus.Failed,
                        FailureReason = "Account inactive",
                        IPAddress = dto.IpAddress,
                        DeviceInfo = dto.DeviceInfo,
                        Browser = dto.Browser,
                        OS = dto.OS
                    });

                    response.Success = false;
                    response.Message = "Your account is inactive.";
                    return response;
                }

                // Locked
                if (user.IsLocked)
                {
                    if (user.LockoutEnd.HasValue &&
                        user.LockoutEnd > DateTime.UtcNow)
                    {
                        var remaining =
                            (int)Math.Ceiling(
                                (user.LockoutEnd.Value - DateTime.UtcNow).TotalMinutes);

                        await RecordLoginHistoryAsync(new LoginHistoryListDto
                        {
                            TenantId = user.TenantId,
                            UserId = user.Id,
                            LoginStatus = LoginStatus.Locked,
                            FailureReason = $"Account locked, {remaining} minute(s) remaining",
                            IPAddress = dto.IpAddress,
                            DeviceInfo = dto.DeviceInfo,
                            Browser = dto.Browser,
                            OS = dto.OS,
                            IsSuspicious = true
                        });

                        response.Success = false;
                        response.Message =
                            $"Account is locked. Try again after {remaining} minute(s).";

                        return response;
                    }

                    user.IsLocked = false;
                    user.AccessFailedCount = 0;
                    user.LockoutEnd = null;

                    await _db.SaveChangesAsync();
                }

                bool validPassword = BCrypt.Net.BCrypt.Verify(
                    dto.Password,
                    user.PasswordHash);

                if (!validPassword)
                {
                    user.AccessFailedCount++;

                    if (user.AccessFailedCount >= 5)
                    {
                        user.IsLocked = true;
                        user.LockoutEnd = DateTime.UtcNow.AddMinutes(30);
                    }

                    await _db.SaveChangesAsync();

                    await RecordLoginHistoryAsync(new LoginHistoryListDto
                    {
                        TenantId = user.TenantId,
                        UserId = user.Id,
                        LoginStatus = LoginStatus.Failed,
                        FailureReason = $"Invalid password (attempt {user.AccessFailedCount}/5)",
                        IPAddress = dto.IpAddress,
                        DeviceInfo = dto.DeviceInfo,
                        Browser = dto.Browser,
                        OS = dto.OS,
                        IsSuspicious = user.AccessFailedCount >= 3
                    });

                    response.Success = false;
                    response.Message =
                        $"Invalid password. Attempt {user.AccessFailedCount}/5";

                    return response;
                }

                user.AccessFailedCount = 0;
                user.IsLocked = false;
                user.LockoutEnd = null;
                user.LastLoginDate = DateTime.UtcNow;
                user.LastLoginIP = dto.IpAddress;

                await _db.SaveChangesAsync();

                await RecordLoginHistoryAsync(new LoginHistoryListDto
                {
                    TenantId = user.TenantId,
                    UserId = user.Id,
                    LoginStatus = LoginStatus.Success,
                    IPAddress = dto.IpAddress,
                    DeviceInfo = dto.DeviceInfo,
                    Browser = dto.Browser,
                    OS = dto.OS,
                    FailureReason="Login Success"
                });

                response.Success = true;
                response.Message = "Login successful.";
                response.Data = await GenerateAuthResponse(user);

                return response;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Login failed.";
                response.Errors.Add(ex.Message);

                return response;
            }
        }

        // ==============================
        // 🕓 LOGIN HISTORY
        // ==============================
        private async Task RecordLoginHistoryAsync(LoginHistoryListDto loginHistory)
        {
            try
            {
                var history = new Domain.Entities.LoginHistory
                {
                    Id = IDManager.GetNewId(new Domain.Entities.LoginHistory()),
                    TenantId = loginHistory.TenantId,
                    UserId = loginHistory.UserId,
                    SessionId = Guid.NewGuid().ToString(),
                    LoginTime = DateTime.UtcNow,
                    LoginStatus = loginHistory.LoginStatus,
                    FailureReason = loginHistory.FailureReason,
                    IPAddress = loginHistory.IPAddress,
                    OS= loginHistory.OS,
                    DeviceInfo=loginHistory.DeviceInfo,
                    Browser= loginHistory.Browser,
                    IsSuspicious = loginHistory.IsSuspicious,
                    CreatedOn = DateTime.UtcNow,
                    CreatedBy = loginHistory.UserId
                };

                await _db.LoginHistories.AddAsync(history);
                await _db.SaveChangesAsync();
            }
            catch
            {
                // Login history is best-effort - never let a logging
                // failure block or fail the actual login attempt.
            }
        }
        //public async Task<AuthResponse> LoginAsync(LoginDto dto)
        //{
        //    try
        //    {
        //    // Get User
        //    var user = await _db.Users
        //        .Include(x => x.Employee)
        //            .ThenInclude(x => x.Designation)
        //        .Include(x => x.UserRoles)
        //            .ThenInclude(x => x.Role)
        //        .Include(x => x.Company)
        //        .Include(x => x.Branch)
        //        .FirstOrDefaultAsync(x => x.Username == dto.Username);

        //    // User Not Found
        //    if (user == null)
        //        throw new Exception("Invalid Username");

        //    // ================================
        //    // CHECK USER LOCKED
        //    // ================================

        //    if (user.IsLocked)
        //    {
        //        // Still Locked
        //        if (user.LockoutEnd.HasValue &&
        //            user.LockoutEnd > DateTime.UtcNow)
        //        {
        //            var remainingMinutes =
        //                (user.LockoutEnd.Value - DateTime.UtcNow).Minutes;

        //            throw new Exception(
        //                $"Account Temporarily Locked\n. Try again after {remainingMinutes} minutes.");
        //        }

        //        // Unlock Automatically
        //        user.IsLocked = false;
        //        user.AccessFailedCount = 0;
        //        user.LockoutEnd = null;

        //        _db.Users.Update(user);
        //        await _db.SaveChangesAsync();
        //    }

        //    // ================================
        //    // VERIFY PASSWORD
        //    // ================================

        //    bool isValidPassword = BCrypt.Net.BCrypt.Verify(
        //        dto.Password,
        //        user.PasswordHash);

        //    // ================================
        //    // INVALID PASSWORD
        //    // ================================

        //    if (!isValidPassword)
        //    {
        //        user.AccessFailedCount += 1;

        //        // Max Attempt = 5
        //        if (user.AccessFailedCount >= 5)
        //        {
        //            user.IsLocked = true;

        //            // Lock For 30 Minutes
        //            user.LockoutEnd = DateTime.UtcNow.AddMinutes(30);
        //        }

        //        _db.Users.Update(user);

        //        await _db.SaveChangesAsync();

        //        throw new Exception(
        //            $"Invalid Password. Attempt {user.AccessFailedCount}/5");
        //    }

        //    // ================================
        //    // LOGIN SUCCESS
        //    // ================================

        //    user.AccessFailedCount = 0;
        //    user.IsLocked = false;
        //    user.LockoutEnd = null;

        //    user.LastLoginDate = DateTime.UtcNow;
        //    user.LastLoginIP = dto.IpAddress;

        //    _db.Users.Update(user);

        //    await _db.SaveChangesAsync();

        //    // Generate Token Response
        //    return await GenerateAuthResponse(user);
        //    }
        //    catch (Exception)
        //    {
        //        return null;
        //    }
        //}

        // ==============================
        // 🔄 REFRESH TOKEN
        // ==============================
        public async Task<AuthResponse> RefreshTokenAsync(string refreshToken)
        {
            try
            {
            var token = _db.RefreshTokens
                .Include(x => x.User)
                .FirstOrDefault(x => x.Token == refreshToken && !x.IsRevoked);

            if (token == null || token.ExpiryDate < DateTime.UtcNow)
                throw new Exception("Invalid refresh token");

            // Sliding expiry: an actively-used refresh token should never
            // force a real re-login just because the calendar moved on -
            // only sustained inactivity (no refresh call for the whole
            // window) or an explicit Logout should end the session. Every
            // successful refresh pushes the token's own expiry forward by
            // the same window again.
            token.ExpiryDate = DateTime.UtcNow.AddDays(RefreshTokenExpiryDays);
            await _db.SaveChangesAsync();

            return await GenerateAuthResponse(token.User, refreshToken);
            }
            catch (Exception)
            {
                return null;
            }
        }

        // ==============================
        // 🚪 LOGOUT
        // ==============================
        public async Task<bool> LogoutAsync(string refreshToken)
        {
            try
            {
                var token = _db.RefreshTokens
                    .FirstOrDefault(x => x.Token == refreshToken);

                if (token == null)
                    return false;

                token.IsRevoked = true;
                await _db.SaveChangesAsync();

                await RecordLogoutAsync(token.UserId);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // Stamps LogoutTime on the most recent still-open (LogoutTime ==
        // null) Successful LoginHistory row for this user - best-effort,
        // same as RecordLoginHistoryAsync, so a logging hiccup never
        // blocks an actual logout.
        private async Task RecordLogoutAsync(string userId)
        {
            try
            {
                var openSession = await _db.LoginHistories
                    .Where(x => !x.IsDeleted
                        && x.UserId == userId
                        && x.LoginStatus == LoginStatus.Success
                        && x.LogoutTime == null)
                    .OrderByDescending(x => x.LoginTime)
                    .FirstOrDefaultAsync();

                if (openSession == null)
                    return;

                openSession.LogoutTime = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }
            catch
            {
                // Best-effort - never let this block the actual logout.
            }
        }

        // ==============================
        // 🔥 COMMON METHOD
        // ==============================
        private async Task<AuthResponse> GenerateAuthResponse(User user,string existingRefreshToken = null)
        {
            // ================================
            // ROLES
            // ================================

            var roles = await _db.UserRoles
                .Where(x => x.UserId == user.Id)
                .Select(x => x.Role.Name)
                .ToListAsync();

            var roleName = roles.FirstOrDefault();

            // ================================
            // PERMISSIONS
            // ================================

            var permissions = await _db.RolePermissions
                .Where(rp => roles.Contains(rp.Role.Name))
                .Select(rp => rp.Permission.Code)
                .Distinct()
                .ToListAsync();

            // ================================
            // ACCESS TOKEN
            // ================================

            var accessToken = _jwt.GenerateAccessToken(
                user,
                roles,
                permissions);

            // ================================
            // REFRESH TOKEN
            // ================================

            string refreshToken = existingRefreshToken;

            if (string.IsNullOrEmpty(refreshToken))
            {
                refreshToken = _jwt.GenerateRefreshToken();

                var refreshTokenEntity = new RefreshToken
                {
                    Id = IDManager.GetNewId(new RefreshToken()),
                    UserId = user.Id,
                    Token = refreshToken,
                    ExpiryDate = DateTime.UtcNow.AddDays(RefreshTokenExpiryDays),
                    IsRevoked = false,
                    TenantId = user.TenantId,
                    CreatedBy = user.Username
                };

                await _db.RefreshTokens.AddAsync(refreshTokenEntity);

                await _db.SaveChangesAsync();
            }

            // ================================
            // RETURN RESPONSE
            // ================================

            return new AuthResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresIn = Convert.ToInt32(_config["Jwt:ExpiryMinutes"]),

                UserId = user.Id,
                EmployeeId = user.EmployeeId,
                TenantId = user.TenantId,
                Username = user.Username,

                FullName = !string.IsNullOrEmpty(user.EmployeeId)
                    ? await GetEmployeeFullName(user.EmployeeId)
                    : user.Username,

                Designation = !string.IsNullOrEmpty(user.EmployeeId)
                    ? await GetDesignationName(user.EmployeeId)
                    : "UNKNOWN",

                CompanyId = user.CompanyId,
                CompanyName = user.Company.Name,
                BranchId = !string.IsNullOrEmpty(user.BranchId)?user.BranchId:"",

                RoleName = roleName,

                Email = user.Email
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
            try
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
            catch (Exception)
            {
                return new List<UserListDto>();
            }
        }

        public async Task<UserListDto?> GetByIdAsync(string id)
        {
            try
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
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<UserListDto?> GetUserDetailsByEmpIdAsync(string empId)
        {
            try
            {
            return await _db.Users
                .AsNoTracking()
                .Include(x => x.UserRoles)
                    .ThenInclude(ur => ur.Role)

                // Filter
                .Where(x => x.EmployeeId == empId)

                // Select DTO
                .Select(x => new UserListDto
                {
                    Id = x.Id,

                    // Role (First Role)
                    RoleId = x.UserRoles
                        .Select(r => r.RoleId)
                        .FirstOrDefault(),

                    // Security
                    EmailConfirmed = x.EmailConfirmed,
                    PhoneConfirmed = x.PhoneConfirmed,
                })

                .FirstOrDefaultAsync();
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<User> GetUserDetailByIdAsync(string id)
        {
            try
            {
            return await _db.Users.AsNoTracking()
            .Include(x => x.UserRoles)
            .FirstOrDefaultAsync(x => x.Id == id);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<User> GettUserDetailByUsernameAsync(string username)
        {
            try
            {
            return await _db.Users
                .Include(x => x.UserRoles)
                .FirstOrDefaultAsync(x => x.Username == username);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<bool> UpdateUserAsync(User user)
        {
            try
            {
            var existingUser = await _db.Users
            .FirstOrDefaultAsync(x => x.Id == user.Id);

            if (existingUser == null)
                return false;

            existingUser.Username = user.Username;
            existingUser.Email = user.Email;
            existingUser.PhoneNumber = user.PhoneNumber;
            existingUser.CompanyId = user.CompanyId;
            existingUser.BranchId = user.BranchId;
            existingUser.EmployeeId = user.EmployeeId;
            existingUser.EmailConfirmed = user.EmailConfirmed;
            existingUser.PhoneConfirmed = user.PhoneConfirmed;

            existingUser.ModifiedOn = DateTime.Now;
            existingUser.ModifiedBy = user.ModifiedBy;

            _db.Users.Update(existingUser);

            await _db.SaveChangesAsync();

            return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<ChangePasswordResultDto> ChangePasswordAsync(string userId, string oldPassword, string newPassword)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
                    return new ChangePasswordResultDto { Success = false, Message = "New password must be at least 6 characters." };

                var user = await _db.Users
                    .FirstOrDefaultAsync(x => x.Id == userId);

                if (user == null)
                    return new ChangePasswordResultDto { Success = false, Message = "User not found." };

                // ================================
                // VERIFY CURRENT PASSWORD
                // ================================
                // BCrypt.Verify returns true when oldPassword matches the
                // stored hash - the change must only proceed in that case
                // (this was previously inverted, which rejected every
                // correct password and silently allowed wrong ones through).
                bool oldPasswordMatches = BCrypt.Net.BCrypt.Verify(
                    oldPassword,
                    user.PasswordHash);

                if (!oldPasswordMatches)
                    return new ChangePasswordResultDto { Success = false, Message = "Current password is incorrect." };

                if (BCrypt.Net.BCrypt.Verify(newPassword, user.PasswordHash))
                    return new ChangePasswordResultDto { Success = false, Message = "New password must be different from the current password." };

                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword, workFactor: 12);

                await _db.SaveChangesAsync();

                return new ChangePasswordResultDto { Success = true, Message = "Password changed successfully." };
            }
            catch (Exception)
            {
                return new ChangePasswordResultDto { Success = false, Message = "Something went wrong while changing the password." };
            }
        }

        // ==============================
        // 🔑 FORGOT PASSWORD (self-service, no email/SMS infra yet)
        // ==============================
        // Identity is verified server-side by requiring BOTH the Username
        // and the Email already on file to match the same account - never
        // trust a client-supplied user id here, and never reveal which of
        // the two fields was wrong (that would let an attacker enumerate
        // valid usernames/emails one field at a time).
        public async Task<ForgotPasswordResultDto> ForgotPasswordAsync(string username, string email, string newPassword)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(email))
                    return new ForgotPasswordResultDto { Success = false, Message = "Username and email are required." };

                if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
                    return new ForgotPasswordResultDto { Success = false, Message = "New password must be at least 6 characters." };

                var user = await _db.Users.FirstOrDefaultAsync(x =>
                    x.Username.ToLower() == username.Trim().ToLower() &&
                    x.Email.ToLower() == email.Trim().ToLower());

                if (user == null)
                    return new ForgotPasswordResultDto { Success = false, Message = "We couldn't find an account matching that username and email." };

                if (!user.IsActive)
                    return new ForgotPasswordResultDto { Success = false, Message = "This account is inactive. Please contact your administrator." };

                if (BCrypt.Net.BCrypt.Verify(newPassword, user.PasswordHash))
                    return new ForgotPasswordResultDto { Success = false, Message = "New password must be different from the current password." };

                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword, workFactor: 12);

                // A successful self-service reset is a reasonable moment to
                // also clear any lockout, so a locked-out user isn't left
                // stuck after proving who they are.
                user.IsLocked = false;
                user.AccessFailedCount = 0;
                user.LockoutEnd = null;

                user.ModifiedOn = DateTime.UtcNow;

                await _db.SaveChangesAsync();

                return new ForgotPasswordResultDto { Success = true, Message = "Password reset successfully. You can now log in with your new password." };
            }
            catch (Exception)
            {
                return new ForgotPasswordResultDto { Success = false, Message = "Something went wrong while resetting the password." };
            }
        }
    }
}
