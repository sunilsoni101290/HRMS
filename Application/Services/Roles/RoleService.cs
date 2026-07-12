using Application.DTOs.Roles;
using Application.Interfaces.Roles;
using Domain.Entities;
using Infrastructure;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.Roles
{
    public class RoleService : IRoleService
    {
        private readonly ApplicationDbContext _context;

        public RoleService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<RoleListDto>> GetAllAsync()
        {
            try
            {
                var roles = await _context.Roles
                    .AsNoTracking()
                    .Where(x => !x.IsDeleted)
                    .OrderBy(x => x.Name)
                    .ToListAsync();

                var userCounts = await _context.UserRoles
                    .AsNoTracking()
                    .Where(x => !x.IsDeleted)
                    .GroupBy(x => x.RoleId)
                    .Select(g => new { RoleId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.RoleId, x => x.Count);

                var permCounts = await _context.RolePermissions
                    .AsNoTracking()
                    .Where(x => !x.IsDeleted && x.IsAllowed)
                    .GroupBy(x => x.RoleId)
                    .Select(g => new { RoleId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.RoleId, x => x.Count);

                return roles.Select(r => new RoleListDto
                {
                    Id = r.Id,
                    Name = r.Name,
                    Code = r.Code,
                    Description = r.Description,
                    IsActive = r.IsActive,
                    UserCount = userCounts.TryGetValue(r.Id, out var uc) ? uc : 0,
                    PermissionCount = permCounts.TryGetValue(r.Id, out var pc) ? pc : 0,
                    CreatedOn = r.CreatedOn
                }).ToList();
            }
            catch (Exception)
            {
                return new List<RoleListDto>();
            }
        }

        public async Task<RoleDto> GetByIdAsync(string id)
        {
            try
            {
                var r = await _context.Roles.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

                if (r == null) return null;

                return new RoleDto
                {
                    Id = r.Id,
                    Name = r.Name,
                    Code = r.Code,
                    Description = r.Description,
                    IsActive = r.IsActive,
                    TenantId = r.TenantId
                };
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<RoleDetailDto> GetDetailAsync(string id)
        {
            try
            {
                var role = await _context.Roles.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

                if (role == null) return null;

                var allPermissions = await _context.Permissions
                    .AsNoTracking()
                    .Where(x => !x.IsDeleted)
                    .OrderBy(x => x.Module).ThenBy(x => x.DisplayOrder)
                    .ToListAsync();

                var assignedPermissionIds = await _context.RolePermissions
                    .AsNoTracking()
                    .Where(x => x.RoleId == id && !x.IsDeleted && x.IsAllowed)
                    .Select(x => x.PermissionId)
                    .ToListAsync();

                var assignedSet = assignedPermissionIds.ToHashSet();

                var featureNames = await _context.AppFeatures
                    .AsNoTracking()
                    .Where(x => !x.IsDeleted)
                    .Select(x => new { x.Code, x.Name })
                    .ToDictionaryAsync(x => x.Code, x => x.Name);

                var groups = allPermissions
                    .GroupBy(p => string.IsNullOrEmpty(p.Module) ? "Other" : p.Module)
                    .Select(g => new RolePermissionGroupDto
                    {
                        Module = g.Key,
                        Permissions = g.Select(p => new RolePermissionItemDto
                        {
                            PermissionId = p.Id,
                            PermissionName = p.Name,
                            Action = p.Action,
                            FeatureId = p.FeatureId,
                            FeatureName = !string.IsNullOrEmpty(p.FeatureId) && featureNames.TryGetValue(p.FeatureId, out var fn)
                                ? fn : p.FeatureId,
                            IsAssigned = assignedSet.Contains(p.Id)
                        }).ToList()
                    })
                    .OrderBy(g => g.Module)
                    .ToList();

                var users = await _context.UserRoles
                    .AsNoTracking()
                    .Where(x => x.RoleId == id && !x.IsDeleted)
                    .Include(x => x.User).ThenInclude(u => u.Employee)
                    .Select(x => new RoleUserSummaryDto
                    {
                        UserId = x.User.Id,
                        Username = x.User.Username,
                        EmployeeName = x.User.Employee != null
                            ? (x.User.Employee.FirstName + " " + x.User.Employee.LastName)
                            : null,
                        IsActive = x.User.IsActive
                    })
                    .ToListAsync();

                return new RoleDetailDto
                {
                    Id = role.Id,
                    Name = role.Name,
                    Code = role.Code,
                    Description = role.Description,
                    IsActive = role.IsActive,
                    PermissionGroups = groups,
                    Users = users
                };
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<string> CreateAsync(RoleDto dto)
        {
            try
            {
                var exists = await _context.Roles
                    .AnyAsync(x => !x.IsDeleted && x.Name.ToLower() == dto.Name.ToLower());

                if (exists)
                    return null;

                var entity = new Role
                {
                    Id = IDManager.GetNewId(new Role()),
                    Name = dto.Name,
                    Code = string.IsNullOrWhiteSpace(dto.Code)
                        ? dto.Name.ToUpper().Replace(" ", "_")
                        : dto.Code,
                    Description = dto.Description,
                    IsActive = dto.IsActive,
                    TenantId = dto.TenantId,
                    CreatedBy = dto.CreatedBy
                };

                await _context.Roles.AddAsync(entity);
                await _context.SaveChangesAsync();

                return entity.Id;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<string> UpdateAsync(string id, RoleDto dto)
        {
            try
            {
                var entity = await _context.Roles.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
                if (entity == null) return null;

                entity.Name = dto.Name;
                entity.Description = dto.Description;
                entity.IsActive = dto.IsActive;
                entity.ModifiedBy = dto.ModifiedBy;
                entity.ModifiedOn = DateTime.UtcNow;
                // Code is intentionally immutable after creation - several
                // places in the app (menu visibility, ESS role checks)
                // key off the Code, not the Id.

                await _context.SaveChangesAsync();
                return entity.Id;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<bool> ToggleActiveAsync(string id)
        {
            try
            {
                var entity = await _context.Roles.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
                if (entity == null) return false;

                entity.IsActive = !entity.IsActive;
                entity.ModifiedOn = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> DeleteAsync(string id)
        {
            try
            {
                var entity = await _context.Roles.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
                if (entity == null) return false;

                // Don't silently orphan users still assigned to this role -
                // an admin has to reassign them first.
                var hasUsers = await _context.UserRoles
                    .AnyAsync(x => x.RoleId == id && !x.IsDeleted);

                if (hasUsers)
                    return false;

                entity.IsDeleted = true;
                entity.IsActive = false;
                entity.ModifiedOn = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> AssignPermissionsAsync(AssignRolePermissionsRequestDto request)
        {
            try
            {
                var role = await _context.Roles.FirstOrDefaultAsync(x => x.Id == request.RoleId && !x.IsDeleted);
                if (role == null) return false;

                var existing = _context.RolePermissions.Where(x => x.RoleId == request.RoleId);
                _context.RolePermissions.RemoveRange(existing);

                var requestedIds = (request.PermissionIds ?? new List<string>()).Distinct().ToList();

                if (requestedIds.Count > 0)
                {
                    var validPermissionIds = await _context.Permissions
                        .Where(p => requestedIds.Contains(p.Id) && !p.IsDeleted)
                        .Select(p => p.Id)
                        .ToListAsync();

                    var newLinks = validPermissionIds.Select(pid => new RolePermission
                    {
                        Id = IDManager.GetNewId(new RolePermission()),
                        RoleId = request.RoleId,
                        PermissionId = pid,
                        IsAllowed = true,
                        CreatedBy = request.ModifiedBy
                    });

                    await _context.RolePermissions.AddRangeAsync(newLinks);
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
