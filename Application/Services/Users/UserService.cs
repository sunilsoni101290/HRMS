using Application.DTOs.Auth;
using Application.DTOs.Users;
using Application.Interfaces.Users;
using Domain.Entities;
using Domain.Helper;
using Infrastructure;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.Users
{
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;

        public UserService(ApplicationDbContext context)
        {
            _context = context;
        }

        #region Get All

        public async Task<List<UserListDto>> GetAllAsync()
        {
            try
            {
                return await _context.Users
                    .AsNoTracking()
                    .Include(x => x.Employee).ThenInclude(e => e.Designation)
                    .Include(x => x.UserRoles).ThenInclude(ur => ur.Role)
                    .Include(x => x.Company)
                    .Include(x => x.Branch)
                    .Include(x => x.Tenant)
                    .Where(x => !x.IsDeleted)
                    .OrderByDescending(x => x.CreatedOn)
                    .Select(x => new UserListDto
                    {
                        Id = x.Id,
                        Username = x.Username,
                        Email = x.Email,
                        PhoneNumber = x.PhoneNumber,

                        RoleId = x.UserRoles.Select(ur => ur.RoleId).FirstOrDefault(),
                        RoleName = x.UserRoles.Select(ur => ur.Role.Name).FirstOrDefault(),

                        Designation = x.Employee != null && x.Employee.Designation != null
                            ? x.Employee.Designation.Name : "",

                        TenantId = x.TenantId,
                        TenantName = x.Tenant != null ? x.Tenant.Name : null,

                        CompanyId = x.CompanyId,
                        CompanyName = x.Company != null ? x.Company.Name : null,

                        BranchId = x.BranchId,
                        BranchName = x.Branch != null ? x.Branch.Name : null,

                        EmployeeId = x.EmployeeId,
                        EmployeeName = x.Employee != null
                            ? x.Employee.FirstName + " " + x.Employee.LastName : null,

                        EmailConfirmed = x.EmailConfirmed,
                        PhoneConfirmed = x.PhoneConfirmed,
                        IsLocked = x.IsLocked,
                        AccessFailedCount = x.AccessFailedCount,
                        LastLoginDate = x.LastLoginDate,
                        LastLoginIP = x.LastLoginIP,
                        IsActive = x.IsActive,
                        CreatedDate = x.CreatedOn,
                        CreatedBy = x.CreatedBy
                    })
                    .ToListAsync();
            }
            catch (Exception)
            {
                return new List<UserListDto>();
            }
        }

        #endregion

        #region Get By Id

        public async Task<UserDto> GetByIdAsync(string id)
        {
            try
            {
                var user = await _context.Users
                    .AsNoTracking()
                    .Include(x => x.UserRoles)
                    .Include(x => x.Employee)
                    .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

                if (user == null)
                    return null;

                return new UserDto
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    RoleId = user.UserRoles.Select(ur => ur.RoleId).FirstOrDefault(),
                    CompanyId = user.CompanyId,
                    BranchId = user.BranchId,
                    EmployeeId = user.EmployeeId,
                    EmployeeName = user.Employee != null
                        ? user.Employee.FirstName + " " + user.Employee.LastName : null,
                    IsActive = user.IsActive,
                    IsLocked = user.IsLocked,
                    TenantId = user.TenantId
                };
            }
            catch (Exception)
            {
                return null;
            }
        }

        #endregion

        #region Create

        public async Task<string> CreateAsync(UserDto dto)
        {
            try
            {
                var exists = await _context.Users
                    .AnyAsync(x => x.Username == dto.Username && !x.IsDeleted);

                if (exists)
                    return "Username already exists.";

                var user = new User
                {
                    Id = IDManager.GetNewId(new User()),
                    Username = dto.Username,
                    Email = dto.Email,
                    PhoneNumber = dto.PhoneNumber,
                    PasswordHash = PasswordHelper.HashPassword(
                        string.IsNullOrWhiteSpace(dto.Password) ? "Welcome@123" : dto.Password),
                    EmailConfirmed = true,
                    PhoneConfirmed = false,
                    IsLocked = false,
                    TenantId = dto.TenantId,
                    CompanyId = dto.CompanyId,
                    BranchId = dto.BranchId,
                    EmployeeId = dto.EmployeeId,
                    IsActive = dto.IsActive,
                    CreatedBy = dto.CreatedBy
                };

                await _context.Users.AddAsync(user);

                if (!string.IsNullOrEmpty(dto.RoleId))
                {
                    await _context.UserRoles.AddAsync(new UserRole
                    {
                        Id = IDManager.GetNewId(new UserRole()),
                        UserId = user.Id,
                        RoleId = dto.RoleId,
                        TenantId = dto.TenantId,
                        CreatedBy = dto.CreatedBy
                    });
                }

                await _context.SaveChangesAsync();
                return user.Id;
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }

        #endregion

        #region Update

        public async Task<string> UpdateAsync(string id, UserDto dto)
        {
            try
            {
                var user = await _context.Users
                    .Include(x => x.UserRoles)
                    .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

                if (user == null)
                    return "User Not Found";

                user.Username = dto.Username;
                user.Email = dto.Email;
                user.PhoneNumber = dto.PhoneNumber;
                user.CompanyId = dto.CompanyId;
                user.BranchId = dto.BranchId;
                user.EmployeeId = dto.EmployeeId;
                user.IsActive = dto.IsActive;
                user.IsLocked = dto.IsLocked;
                user.TenantId = dto.TenantId;
                user.ModifiedBy = dto.ModifiedBy;
                user.ModifiedOn = DateTime.UtcNow;

                // Replace role
                if (!string.IsNullOrEmpty(dto.RoleId))
                {
                    _context.UserRoles.RemoveRange(user.UserRoles);
                    await _context.UserRoles.AddAsync(new UserRole
                    {
                        Id = IDManager.GetNewId(new UserRole()),
                        UserId = user.Id,
                        RoleId = dto.RoleId,
                        TenantId = dto.TenantId,
                        CreatedBy = dto.ModifiedBy
                    });
                }

                _context.Users.Update(user);
                await _context.SaveChangesAsync();
                return user.Id;
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }

        #endregion

        #region Reset Password

        public async Task<bool> ResetPasswordAsync(string id, string newPassword)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

                if (user == null || string.IsNullOrWhiteSpace(newPassword))
                    return false;

                user.PasswordHash = PasswordHelper.HashPassword(newPassword);
                user.ModifiedOn = DateTime.UtcNow;

                _context.Users.Update(user);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        #endregion

        #region Toggle Active / Lock

        public async Task<bool> ToggleActiveAsync(string id)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

                if (user == null) return false;

                user.IsActive = !user.IsActive;
                user.ModifiedOn = DateTime.UtcNow;

                _context.Users.Update(user);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> ToggleLockAsync(string id)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

                if (user == null) return false;

                user.IsLocked = !user.IsLocked;
                if (!user.IsLocked) user.AccessFailedCount = 0;
                user.ModifiedOn = DateTime.UtcNow;

                _context.Users.Update(user);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        #endregion

        #region Delete (Soft)

        public async Task<bool> DeleteAsync(string id)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

                if (user == null) return false;

                user.IsDeleted = true;
                user.IsActive = false;
                user.ModifiedOn = DateTime.UtcNow;

                _context.Users.Update(user);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        #endregion
    }
}
