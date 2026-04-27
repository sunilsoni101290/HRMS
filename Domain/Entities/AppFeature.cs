using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Domain.Entities
{
    public class AppFeature : BaseEntity
    {
        [Required, MaxLength(150)]
        public string Name { get; set; }
        // e.g. Employee Management

        [Required, MaxLength(100)]
        public string Code { get; set; }
        // EMPLOYEE, PAYROLL, LEAVE

        public string Module { get; set; }
        // HRMS / Billing / Inventory

        public string? Description { get; set; }

        public string? ParentFeatureId { get; set; }
        public virtual AppFeature ParentFeature { get; set; }

        public ICollection<AppFeature> ChildFeatures { get; set; }

        public int DisplayOrder { get; set; }
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
