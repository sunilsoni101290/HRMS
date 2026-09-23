using Application.Common.Exceptions;
using Application.DTOs;
using Domain.Entities;
using Domain.Helper;
using Infrastructure;
using System;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Data;
using Application.Interfaces.Masters;

namespace Application.Services.Masters
{
    public class AppFeatureService : IAppFeatureService
    {
        private readonly ApplicationDbContext _context;

        public AppFeatureService(ApplicationDbContext context)
        {
            _context = context;
        }

        // ======================================================
        // GET ALL
        // ======================================================

        public async Task<List<AppFeatureDto>> GetAllAsync(string actingUserId)
        {
            await EnsurePermissionAsync(actingUserId, Actions.View);

            try
            {
            var data = await _context.AppFeatures
                .Include(x => x.ParentFeature)
                .OrderBy(x => x.DisplayOrder)
                .ToListAsync();

            return data.Select(x => new AppFeatureDto
            {
                Id = x.Id,
                Name = x.Name,
                Code = x.Code,
                Module = x.Module,
                Description = x.Description,

                ParentFeatureId = x.ParentFeatureId,
                ParentFeatureName = x.ParentFeature != null
                                        ? x.ParentFeature.Name
                                        : null,

                DisplayOrder = x.DisplayOrder,

                ControllerName = x.ControllerName,
                ActionName = x.ActionName,
                AreaName = x.AreaName,
                // Effective URL (manual override if set, else the
                // Area/Controller/Action convention) - matches what the
                // listing/Edit screen should actually show.
                Url = x.ResolvedUrl,

                IsVisible = x.IsVisible,
                IsMenu = x.IsMenu,
                IsActive = x.IsActive,

                Icon = x.Icon,
                BadgeText = x.BadgeText,

                CanView = x.CanView,
                CanAdd = x.CanAdd,
                CanEdit = x.CanEdit,
                CanDelete = x.CanDelete,
                CanApprove = x.CanApprove,
                CanExport = x.CanExport,
                CanPrint = x.CanPrint,

                AppFeatureType = x.AppFeatureType,
                IsHRMSFeature = x.IsHRMSFeature

            }).ToList();
            }
            catch (Exception)
            {
                return new List<AppFeatureDto>();
            }
        }

        // ======================================================
        // GET BY ID
        // ======================================================

        public async Task<AppFeatureDto?> GetByIdAsync(string id, string actingUserId)
        {
            await EnsurePermissionAsync(actingUserId, Actions.View);

            try
            {
            var x = await _context.AppFeatures
                .Include(a => a.ParentFeature)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (x == null)
                return null;

            return new AppFeatureDto
            {
                Id = x.Id,
                Name = x.Name,
                Code = x.Code,
                Module = x.Module,
                Description = x.Description,

                ParentFeatureId = x.ParentFeatureId,
                ParentFeatureName = x.ParentFeature?.Name,

                DisplayOrder = x.DisplayOrder,

                ControllerName = x.ControllerName,
                ActionName = x.ActionName,
                AreaName = x.AreaName,
                // Effective URL - see GetAllAsync's comment above. This is
                // what pre-populates the Edit screen's Url field, so it
                // always loads the true current value (manual override,
                // or the computed one if none was ever set).
                Url = x.ResolvedUrl,

                IsVisible = x.IsVisible,
                IsMenu = x.IsMenu,
                IsActive = x.IsActive,

                Icon = x.Icon,
                BadgeText = x.BadgeText,

                CanView = x.CanView,
                CanAdd = x.CanAdd,
                CanEdit = x.CanEdit,
                CanDelete = x.CanDelete,
                CanApprove = x.CanApprove,
                CanExport = x.CanExport,
                CanPrint = x.CanPrint,

                AppFeatureType = x.AppFeatureType,
                IsHRMSFeature = x.IsHRMSFeature,

                CreatedBy = x.CreatedBy,
                ModifiedBy = x.ModifiedBy,
                ModifiedOn = x.ModifiedOn,
            };
            }
            catch (Exception)
            {
                return null;
            }
        }

        // ======================================================
        // CREATE
        // ======================================================

        public async Task<AppFeatureDto> CreateAsync(AppFeatureDto dto, string actingUserId)
        {
            await EnsurePermissionAsync(actingUserId, Actions.Create);

            try
            {
            var entity = new AppFeature
            {
                Id=IDManager.GetNewId(new AppFeature()),
                Name = dto.Name,
                Code = dto.Code,
                Module = dto.Module,
                Description = dto.Description,

                ParentFeatureId = dto.ParentFeatureId,
                DisplayOrder = dto.DisplayOrder,

                ControllerName = dto.ControllerName,
                ActionName = dto.ActionName,
                AreaName = dto.AreaName,
                Url = NormalizeUrl(dto.Url),

                IsVisible = dto.IsVisible,
                IsMenu = dto.IsMenu,
                IsActive = dto.IsActive,

                Icon = dto.Icon,
                BadgeText = dto.BadgeText,

                CanView = dto.CanView,
                CanAdd = dto.CanAdd,
                CanEdit = dto.CanEdit,
                CanDelete = dto.CanDelete,
                CanApprove = dto.CanApprove,
                CanExport = dto.CanExport,
                CanPrint = dto.CanPrint,

                AppFeatureType = dto.AppFeatureType,
                IsHRMSFeature = dto.IsHRMSFeature,

                CreatedBy = dto.CreatedBy,
            };

            await _context.AppFeatures.AddAsync(entity);
            await _context.SaveChangesAsync();

            dto.Id = entity.Id;

            return dto;
            }
            catch (Exception)
            {
                return null;
            }
        }

        // ======================================================
        // UPDATE
        // ======================================================

        public async Task<AppFeatureDto?> UpdateAsync(AppFeatureDto dto, string actingUserId)
        {
            await EnsurePermissionAsync(actingUserId, Actions.Edit);

            try
            {
            var entity = await _context.AppFeatures
                .FirstOrDefaultAsync(x => x.Id == dto.Id);

            if (entity == null)
                return null;

            entity.Name = dto.Name;
            entity.Code = dto.Code;
            entity.Module = dto.Module;
            entity.Description = dto.Description;

            entity.ParentFeatureId = dto.ParentFeatureId;
            entity.DisplayOrder = dto.DisplayOrder;

            entity.ControllerName = dto.ControllerName;
            entity.ActionName = dto.ActionName;
            entity.AreaName = dto.AreaName;
            // Persist exactly what was submitted (trimmed / blank -> null).
            // Url is now a real, mapped column (Domain/Entities/
            // AppFeature.cs) instead of the old [NotMapped] computed
            // property, so an edited value here actually survives the
            // save and is not silently overwritten by the
            // Area/Controller/Action convention on the next load - see
            // GetByIdAsync's ResolvedUrl comment above for how it's
            // re-populated when the field is left blank.
            entity.Url = NormalizeUrl(dto.Url);

            entity.IsVisible = dto.IsVisible;
            entity.IsMenu = dto.IsMenu;
            entity.IsActive = dto.IsActive;

            entity.Icon = dto.Icon;
            entity.BadgeText = dto.BadgeText;

            entity.CanView = dto.CanView;
            entity.CanAdd = dto.CanAdd;
            entity.CanEdit = dto.CanEdit;
            entity.CanDelete = dto.CanDelete;
            entity.CanApprove = dto.CanApprove;
            entity.CanExport = dto.CanExport;
            entity.CanPrint = dto.CanPrint;

            entity.AppFeatureType = dto.AppFeatureType;
            entity.IsHRMSFeature = dto.IsHRMSFeature;

            entity.ModifiedBy = dto.ModifiedBy;
            entity.ModifiedOn = dto.ModifiedOn;

            _context.AppFeatures.Update(entity);

            await _context.SaveChangesAsync();

            return dto;
            }
            catch (Exception)
            {
                return null;
            }
        }

        // ======================================================
        // HELPERS
        // ======================================================

        // Trims a submitted Url and turns blank/whitespace into null, so
        // "not set" is stored consistently as NULL (falls back to
        // AppFeature.ComputedUrl) rather than an empty string.
        private static string? NormalizeUrl(string? url)
            => string.IsNullOrWhiteSpace(url) ? null : url.Trim();

        // ======================================================
        // DELETE
        // ======================================================

        public async Task<bool> DeleteAsync(string id, string actingUserId)
        {
            await EnsurePermissionAsync(actingUserId, Actions.Delete);

            try
            {
            var entity = await _context.AppFeatures
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                return false;

            _context.AppFeatures.Remove(entity);

            await _context.SaveChangesAsync();

            return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // =========================================================
        // LOAD MENU FROM DATABASE
        // =========================================================

        public async Task<List<AppFeatureDto>> GetMenuAsync()
        {
            try
            {
            return await _context.AppFeatures
                .AsNoTracking()
                .Where(x => x.IsActive && x.IsMenu)
                .OrderBy(x => x.DisplayOrder)
                .Select(x => new AppFeatureDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Code = x.Code,
                    ParentFeatureId = x.ParentFeatureId,
                    Icon = x.Icon,

                    ControllerName = x.ControllerName,
                    ActionName = x.ActionName,
                    AreaName = x.AreaName,

                    // Prefer the manually-set Url override; fall back to
                    // the Area/Controller/Action convention when it's
                    // blank. Previously this always rebuilt the URL from
                    // Area/Controller/Action and ignored any stored
                    // override, so a custom Url (e.g. an absolute link)
                    // never actually took effect in the sidebar menu.
                    Url = !string.IsNullOrEmpty(x.Url)
                            ? x.Url
                            : (!string.IsNullOrEmpty(x.AreaName)
                                ? "/" + x.AreaName + "/" + x.ControllerName + "/" + x.ActionName
                                : "/" + x.ControllerName + "/" + x.ActionName),

                    DisplayOrder = x.DisplayOrder
                })
                .ToListAsync();
            }
            catch (Exception)
            {
                return new List<AppFeatureDto>();
            }
        }

        // ======================================================
        // GET MENU (role-based)
        //   - Super Admin / System Configurator → full menu
        //   - No roles     → full menu (avoids lock-out during rollout)
        //   - Otherwise    → only features the role can View, plus the
        //                    parent groups that still have a visible child
        // ======================================================
        public async Task<List<AppFeatureDto>> GetMenuByUserAsync(string? userId)
        {
            try
            {
            var all = await GetMenuAsync();

            if (string.IsNullOrEmpty(userId))
                return all;

            var roleIds = await _context.UserRoles
                .AsNoTracking()
                .Where(ur => ur.UserId == userId && !ur.IsDeleted)
                .Select(ur => ur.RoleId)
                .ToListAsync();

            // No role mapping → don't hide anything.
            if (roleIds.Count == 0)
                return all;

            var roleCodes = await _context.Roles
                .AsNoTracking()
                .Where(r => roleIds.Contains(r.Id))
                .Select(r => r.Code)
                .ToListAsync();

            if (roleCodes.Contains(ConstantHelper.SUPER_ADMIN) ||
                roleCodes.Contains(ConstantHelper.HR_MANAGER) ||
                roleCodes.Contains(ConstantHelper.SYSTEM_CONFIGURATOR))
                return all;

            // Feature codes this user is allowed to view
            var allowedFeatureCodes = await (
                from rp in _context.RolePermissions.AsNoTracking()
                join p in _context.Permissions.AsNoTracking() on rp.PermissionId equals p.Id
                where roleIds.Contains(rp.RoleId)
                      && rp.IsAllowed
                      && !rp.IsDeleted
                      && (p.Action == "View" || p.Action == "Index")
                select p.FeatureId
            ).Distinct().ToListAsync();

            var allowedSet = allowedFeatureCodes.ToHashSet();

            // Visible leaves (features that point to a screen)
            var visibleLeaves = all
                .Where(f => !string.IsNullOrEmpty(f.ControllerName)
                         && allowedSet.Contains(f.Code))
                .ToList();

            var visibleLeafIds = visibleLeaves
                .Select(f => f.Id)
                .ToHashSet();

            // Parent groups (no controller) with at least one visible child
            var visibleParents = all
                .Where(f => string.IsNullOrEmpty(f.ControllerName)
                         && all.Any(c => c.ParentFeatureId == f.Id
                                      && visibleLeafIds.Contains(c.Id)))
                .ToList();

            return visibleParents
                .Concat(visibleLeaves)
                .OrderBy(f => f.DisplayOrder)
                .ToList();
            }
            catch (Exception)
            {
                return new List<AppFeatureDto>();
            }
        }

        // ======================================================
        // MENU BAR REDESIGN - Favorites / Quick Access
        // ======================================================

        public async Task<List<AppFeatureDto>> GetFavoritesAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return new List<AppFeatureDto>();

            try
            {
                var pinnedFeatureIds = await _context.UserFavoriteMenus
                    .AsNoTracking()
                    .Where(f => f.UserId == userId && !f.IsDeleted)
                    .OrderBy(f => f.DisplayOrder)
                    .Select(f => f.AppFeatureId)
                    .ToListAsync();

                if (pinnedFeatureIds.Count == 0)
                    return new List<AppFeatureDto>();

                // Intersect against the role/permission-filtered menu (not
                // a raw AppFeatures lookup) - a feature pinned before a
                // permission was revoked must not still show up here.
                var allowedMenu = await GetMenuByUserAsync(userId);
                var allowedById = allowedMenu.ToDictionary(f => f.Id, f => f);

                return pinnedFeatureIds
                    .Where(id => allowedById.ContainsKey(id))
                    .Select(id => allowedById[id])
                    .ToList();
            }
            catch (Exception)
            {
                return new List<AppFeatureDto>();
            }
        }

        public async Task<bool> AddFavoriteAsync(string userId, string appFeatureId, string tenantId)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(appFeatureId))
                return false;

            var existing = await _context.UserFavoriteMenus
                .FirstOrDefaultAsync(f => f.UserId == userId && f.AppFeatureId == appFeatureId && !f.IsDeleted);

            // Idempotent - already pinned is success, not a duplicate-key error.
            if (existing != null)
                return true;

            var maxOrder = await _context.UserFavoriteMenus
                .Where(f => f.UserId == userId && !f.IsDeleted)
                .Select(f => (int?)f.DisplayOrder)
                .MaxAsync() ?? 0;

            var favorite = new UserFavoriteMenu
            {
                Id = IDManager.GetNewId(new UserFavoriteMenu()),
                TenantId = tenantId,
                UserId = userId,
                AppFeatureId = appFeatureId,
                DisplayOrder = maxOrder + 1,
                CreatedBy = userId
            };

            await _context.UserFavoriteMenus.AddAsync(favorite);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RemoveFavoriteAsync(string userId, string appFeatureId)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(appFeatureId))
                return false;

            var existing = await _context.UserFavoriteMenus
                .FirstOrDefaultAsync(f => f.UserId == userId && f.AppFeatureId == appFeatureId && !f.IsDeleted);

            // Already not pinned - idempotent success, same reasoning as AddFavoriteAsync.
            if (existing == null)
                return true;

            existing.IsDeleted = true;
            existing.ModifiedOn = DateTime.UtcNow;
            existing.ModifiedBy = userId;

            await _context.SaveChangesAsync();
            return true;
        }

        // ==================================================================
        // PERMISSION CHECK - Feature Management (AppFeatures CRUD) is
        // System Configurator ONLY. Same shape as
        // ErrorLogService.EnsurePermissionAsync / DatabaseManagementService.
        // EnsurePermissionAsync - a real, data-driven RolePermission/
        // Permission check against AppFeatureConstants.APP_FEATURE, not a
        // hard-coded role-name string. In practice only System Configurator
        // ever holds it (see DbSeeder's APP_FEATURE carve-out), but a
        // tenant could grant it to another role later without any code
        // change here. Does NOT gate GetMenuAsync/GetMenuByUserAsync/
        // Favorites above - those are every logged-in user's own sidebar
        // data, not Feature Management.
        // ==================================================================

        private async Task EnsurePermissionAsync(string? actingUserId, string action)
        {
            if (string.IsNullOrEmpty(actingUserId))
                throw new UnauthorizedException("You are not authorized to manage Feature Maintenance.");

            var allowed = await (
                from ur in _context.UserRoles.AsNoTracking()
                join rp in _context.RolePermissions.AsNoTracking().Where(x => x.IsAllowed) on ur.RoleId equals rp.RoleId
                join p in _context.Permissions.AsNoTracking().Where(x =>
                        x.FeatureId == AppFeatureConstants.APP_FEATURE && x.Action == action)
                    on rp.PermissionId equals p.Id
                where ur.UserId == actingUserId
                select p.Id
            ).AnyAsync();

            if (!allowed)
                throw new UnauthorizedException("You are not authorized to manage Feature Maintenance.");
        }
    }
}
