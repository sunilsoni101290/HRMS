using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using static Domain.Enums.EnumExtensions;

namespace Application.DTOs
{
    public class AppFeatureDto
    {
        public string? Id { get; set; }

        [Required]
        [MaxLength(150)]
        [Display(Name = "Feature Name")]
        public string Name { get; set; }

        [Required]
        [MaxLength(100)]
        [Display(Name = "Feature Code")]
        public string Code { get; set; }

        [Required]
        [MaxLength(100)]
        [Display(Name = "Module")]
        public string Module { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        // =========================================
        // MENU HIERARCHY
        // =========================================

        public string? ParentFeatureId { get; set; }

        public string? ParentFeatureName { get; set; }

        public int DisplayOrder { get; set; } = 0;

        // =========================================
        // ROUTING
        // =========================================

        [Required]
        [MaxLength(100)]
        [Display(Name = "Controller")]
        public string ControllerName { get; set; }

        [Required]
        [MaxLength(100)]
        [Display(Name = "Action")]
        public string ActionName { get; set; } = "Index";

        [MaxLength(100)]
        [Display(Name = "Area")]
        public string? AreaName { get; set; }

        public string? Url { get; set; }

        // =========================================
        // UI SETTINGS
        // =========================================

        public bool IsVisible { get; set; } = true;

        public bool IsMenu { get; set; } = true;

        public bool IsActive { get; set; } = true;

        [MaxLength(100)]
        public string? Icon { get; set; }

        [MaxLength(50)]
        public string? BadgeText { get; set; }

        // =========================================
        // ACCESS CONTROL
        // =========================================

        public bool CanView { get; set; } = true;

        public bool CanAdd { get; set; }

        public bool CanEdit { get; set; }

        public bool CanDelete { get; set; }

        public bool CanApprove { get; set; }

        public bool CanExport { get; set; }

        public bool CanPrint { get; set; }

        // =========================================
        // FEATURE TYPE
        // =========================================

        public AppFeatureType? AppFeatureType { get; set; }
        public bool IsHRMSFeature { get; set; } = true;

        // =========================================
        // AUDIT FIELDS
        // =========================================
        public string CreatedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public string? ModifiedBy { get; set; }

    }

    // ==================================================
    // MENU BAR REDESIGN - Favorites / Quick Access
    // ==================================================
    // Request body for POST api/appfeatures/favorites (pin a menu item).
    public class FavoriteMenuRequestDto
    {
        [Required]
        public string UserId { get; set; }

        [Required]
        public string AppFeatureId { get; set; }

        public string? TenantId { get; set; }
    }
}
