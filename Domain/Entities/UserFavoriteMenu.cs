using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities
{
    // ==========================================================================
    // MENU BAR REDESIGN - Favorites / Quick Access
    // ==========================================================================
    // Per-user pinned menu items so a user with 100+ AppFeature menu items
    // (this HRMS has ~20 top-level modules) can reach the 4-5 they actually use
    // every day without walking the full sidebar tree each time. One row per
    // (UserId, AppFeatureId) pin - order is user-controlled via DisplayOrder so
    // a user can drag their own quick-access list into the order they want.
    //
    // Deliberately its own small table rather than reusing RoleFeature/
    // TenantFeature (those two answer "is this feature enabled at all for
    // this role/tenant" - an admin/authorization concern). This is a pure
    // per-user UI preference with no authorization meaning: pinning an item
    // here never grants access to it - GetMenuByUserAsync's existing
    // role/permission filtering is still what decides what a user is even
    // allowed to see, and the Favorites list is always built by intersecting
    // this table with that already-filtered menu (see
    // AppFeatureService.GetFavoritesAsync).
    public class UserFavoriteMenu : BaseEntity
    {
        [Required]
        public string UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public virtual User User { get; set; }

        [Required]
        public string AppFeatureId { get; set; }

        [ForeignKey(nameof(AppFeatureId))]
        public virtual AppFeature AppFeature { get; set; }

        // Order within the user's own Quick Access list (drag-reorder on the
        // frontend just persists new DisplayOrder values back - no separate
        // "move up/down" API needed).
        public int DisplayOrder { get; set; } = 0;

        public override string GetSequencePrefix() => "UFM";
    }
}
