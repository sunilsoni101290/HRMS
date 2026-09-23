using Application.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces.Masters
{
    public interface IAppFeatureService
    {
        // Feature Management CRUD - System Configurator ONLY (see
        // EnsurePermissionAsync in AppFeatureService). actingUserId is
        // always passed explicitly rather than trusted implicitly, same
        // convention as IErrorLogService/IDatabaseManagementService.
        // GetMenuAsync/GetMenuByUserAsync/Favorites below are NOT part of
        // this restriction - every logged-in user needs those for their
        // own sidebar.
        Task<List<AppFeatureDto>> GetAllAsync(string actingUserId);

        Task<AppFeatureDto?> GetByIdAsync(string id, string actingUserId);

        Task<AppFeatureDto> CreateAsync(AppFeatureDto dto, string actingUserId);

        Task<AppFeatureDto?> UpdateAsync(AppFeatureDto dto, string actingUserId);

        Task<bool> DeleteAsync(string id, string actingUserId);
        Task<List<AppFeatureDto>> GetMenuAsync();
        Task<List<AppFeatureDto>> GetMenuByUserAsync(string? userId);

        // ==================================================
        // MENU BAR REDESIGN - Favorites / Quick Access
        // ==================================================
        // Per-user pinned menu items (see Domain/Entities/UserFavoriteMenu.cs).
        // GetFavoritesAsync returns the user's pinned AppFeatureDto rows
        // (Name/Icon/Url populated) already intersected against
        // GetMenuByUserAsync's role/permission-filtered menu, so a favorite
        // pinned before a permission change is revoked never leaks through.
        Task<List<AppFeatureDto>> GetFavoritesAsync(string userId);

        // Idempotent: pinning an already-pinned feature is a no-op success,
        // not an error - the frontend star toggle does not need to track
        // client-side state to avoid a duplicate-key failure.
        Task<bool> AddFavoriteAsync(string userId, string appFeatureId, string tenantId);

        Task<bool> RemoveFavoriteAsync(string userId, string appFeatureId);
    }
}
