using Application.DTOs.Permissions;
using Application.Interfaces.Permissions;
using Domain.Entities;
using Infrastructure;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.Permissions
{
    public class PermissionService : IPermissionService
    {
        private readonly ApplicationDbContext _context;

        public PermissionService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<PermissionListDto>> GetAllAsync()
        {
            try
            {
                var permissions = await _context.Permissions
                    .AsNoTracking()
                    .Where(x => !x.IsDeleted)
                    .OrderBy(x => x.Module).ThenBy(x => x.DisplayOrder)
                    .ToListAsync();

                var featureNames = await _context.AppFeatures
                    .AsNoTracking()
                    .Where(x => !x.IsDeleted)
                    .Select(x => new { x.Code, x.Name })
                    .ToDictionaryAsync(x => x.Code, x => x.Name);

                var roleCounts = await _context.RolePermissions
                    .AsNoTracking()
                    .Where(x => !x.IsDeleted && x.IsAllowed)
                    .GroupBy(x => x.PermissionId)
                    .Select(g => new { PermissionId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.PermissionId, x => x.Count);

                return permissions.Select(p => new PermissionListDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Code = p.Code,
                    Module = p.Module,
                    FeatureId = p.FeatureId,
                    FeatureName = !string.IsNullOrEmpty(p.FeatureId) && featureNames.TryGetValue(p.FeatureId, out var fn)
                        ? fn : p.FeatureId,
                    Action = p.Action,
                    RoleCount = roleCounts.TryGetValue(p.Id, out var rc) ? rc : 0,
                    CreatedOn = p.CreatedOn
                }).ToList();
            }
            catch (Exception)
            {
                return new List<PermissionListDto>();
            }
        }

        public async Task<PermissionDto> GetByIdAsync(string id)
        {
            try
            {
                var p = await _context.Permissions.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

                if (p == null) return null;

                var featureName = !string.IsNullOrEmpty(p.FeatureId)
                    ? await _context.AppFeatures.AsNoTracking()
                        .Where(f => f.Code == p.FeatureId)
                        .Select(f => f.Name)
                        .FirstOrDefaultAsync()
                    : null;

                return new PermissionDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Code = p.Code,
                    Module = p.Module,
                    FeatureId = p.FeatureId,
                    FeatureName = featureName,
                    Action = p.Action,
                    Description = p.Description,
                    DisplayOrder = p.DisplayOrder
                };
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<string> CreateAsync(PermissionDto dto)
        {
            try
            {
                var code = string.IsNullOrWhiteSpace(dto.Code)
                    ? $"{dto.FeatureId}_{dto.Action}".ToUpper()
                    : dto.Code;

                var exists = await _context.Permissions
                    .AnyAsync(x => !x.IsDeleted && x.Code == code);

                if (exists)
                    return null;

                var entity = new Permission
                {
                    Id = IDManager.GetNewId(new Permission()),
                    Name = dto.Name,
                    Code = code,
                    Module = dto.Module,
                    FeatureId = dto.FeatureId,
                    Action = dto.Action,
                    Description = dto.Description,
                    DisplayOrder = dto.DisplayOrder,
                    CreatedBy = dto.CreatedBy
                };

                await _context.Permissions.AddAsync(entity);
                await _context.SaveChangesAsync();

                return entity.Id;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<string> UpdateAsync(string id, PermissionDto dto)
        {
            try
            {
                var entity = await _context.Permissions.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
                if (entity == null) return null;

                entity.Name = dto.Name;
                entity.Module = dto.Module;
                entity.FeatureId = dto.FeatureId;
                entity.Action = dto.Action;
                entity.Description = dto.Description;
                entity.DisplayOrder = dto.DisplayOrder;
                entity.ModifiedBy = dto.ModifiedBy;
                entity.ModifiedOn = DateTime.UtcNow;
                // Code is intentionally immutable - role-permission links
                // and the ReconcilePermissionsAsync seeder key off it.

                await _context.SaveChangesAsync();
                return entity.Id;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<bool> DeleteAsync(string id)
        {
            try
            {
                var entity = await _context.Permissions.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
                if (entity == null) return false;

                entity.IsDeleted = true;
                entity.ModifiedOn = DateTime.UtcNow;

                var links = _context.RolePermissions.Where(x => x.PermissionId == id);
                _context.RolePermissions.RemoveRange(links);

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
