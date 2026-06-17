using Domain.Helper;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using static Domain.Enums.EnumExtensions;

namespace Domain.Entities
{
    public class AppFeature : BaseEntity
    {
        [Required]
        [MaxLength(150)]
        [Display(Name = "Feature Name")]
        public string Name { get; set; }
        // Example: Employee Management

        [Required]
        [MaxLength(100)]
        [Display(Name = "Feature Code")]
        public string Code { get; set; }
        // Example: EMPLOYEE, PAYROLL, LEAVE

        [Required]
        [MaxLength(100)]
        [Display(Name = "Module")]
        public string Module { get; set; } = AppFeatureConstants.HRMS;
        // Example: HRMS, Billing, Inventory

        [MaxLength(500)]
        public string? Description { get; set; }

        // =========================================
        // MENU HIERARCHY
        // =========================================

        public string? ParentFeatureId { get; set; }

        [ForeignKey(nameof(ParentFeatureId))]
        public virtual AppFeature? ParentFeature { get; set; }

        public virtual ICollection<AppFeature>? ChildFeatures { get; set; }

        public int DisplayOrder { get; set; } = 0;

        // =========================================
        // ROUTING (Normalized URL Structure)
        // =========================================

        [Required]
        [MaxLength(100)]
        [Display(Name = "Controller")]
        public string ControllerName { get; set; }
        // Example: Employee

        [Required]
        [MaxLength(100)]
        [Display(Name = "Action")]
        public string ActionName { get; set; } = "Index";
        // Example: Index

        [MaxLength(100)]
        [Display(Name = "Area")]
        public string? AreaName { get; set; }
        // Example: Admin

        [NotMapped]
        public string Url
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(AreaName))
                {
                    return $"/{AreaName}/{ControllerName}/{ActionName}";
                }

                return $"/{ControllerName}/{ActionName}";
            }
        }

        // =========================================
        // UI SETTINGS
        // =========================================

        public bool IsVisible { get; set; } = true;

        public bool IsMenu { get; set; } = true;

        public bool IsActive { get; set; } = true;

        [MaxLength(100)]
        public string? Icon { get; set; }
        // Example: bi bi-people-fill

        [MaxLength(50)]
        public string? BadgeText { get; set; }
        // Example: New

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

        [MaxLength(100)]
        public AppFeatureType? AppFeatureType { get; set; } // Master / Transaction / Report / Setting

        public bool IsHRMSFeature { get; set; } = true;

        public override string GetSequencePrefix() => "APF";
    }

    public class RoleFeature : BaseEntity
    {
        public string RoleId { get; set; }
        public virtual Role Role { get; set; }

        public string AppFeatureId { get; set; }
        public virtual AppFeature AppFeature { get; set; }

        public bool IsEnabled { get; set; } = true;
        public override string GetSequencePrefix() => "RF";
    }

    public class TenantFeature : BaseEntity
    {
        public string TenantId { get; set; }
        public virtual Tenant Tenant { get; set; }

        public string AppFeatureId { get; set; }
        public virtual AppFeature AppFeature { get; set; }

        public bool IsEnabled { get; set; } = true;

        public DateTime? ExpiryDate { get; set; }
        public override string GetSequencePrefix() => "TNF";
    }
}
