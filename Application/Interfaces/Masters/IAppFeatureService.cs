using Application.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces.Masters
{
    public interface IAppFeatureService
    {
        Task<List<AppFeatureDto>> GetAllAsync();

        Task<AppFeatureDto?> GetByIdAsync(string id);

        Task<AppFeatureDto> CreateAsync(AppFeatureDto dto);

        Task<AppFeatureDto?> UpdateAsync(AppFeatureDto dto);

        Task<bool> DeleteAsync(string id);
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
