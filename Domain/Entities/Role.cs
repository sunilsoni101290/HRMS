using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Domain.Entities
{
    public class Role : BaseEntity
    {
        [Required, MaxLength(100)]
        public string Name { get; set; }
        public string Code { get; set; }
        public string? Description { get; set; }
        public string TenantId { get; set; }

        // ✅ Correct
        public ICollection<UserRole> UserRoles { get; set; }
        public ICollection<RolePermission> RolePermissions { get; set; }
        public override string GetSequencePrefix() => "ROL";
    }

    public class UserRole : BaseEntity
    {
        public string UserId { get; set; }
        public User User { get; set; }

        public string RoleId { get; set; }
        public Role Role { get; set; }
        public override string GetSequencePrefix() => "UR";
    }


    public class Permission : BaseEntity
    {
        [Required, MaxLength(150)]
        public string Name { get; set; }
        // e.g. "Create Employee"

        [Required, MaxLength(100)]
        public string Code { get; set; }
        // EMPLOYEE_CREATE

        public string Module { get; set; }
        // HRMS / Billing / Inventory

        public string FeatureId { get; set; }
        // Employee / Payroll / Leave

        public string Action { get; set; }
        // Create / View / Edit / Delete / Approve

        public string? Description { get; set; }

        public int DisplayOrder { get; set; }
        public ICollection<RolePermission> RolePermissions { get; set; }
        public override string GetSequencePrefix() => "P";
    }

    public class RolePermission : BaseEntity
    {
        public string RoleId { get; set; }
        public virtual Role Role { get; set; }

        public string PermissionId { get; set; }
        public virtual Permission Permission { get; set; }

        public bool IsAllowed { get; set; } = true;
        public override string GetSequencePrefix() => "RP";
    }
}
