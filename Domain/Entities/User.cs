using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Text;

namespace Domain.Entities
{
    public class User : BaseEntity
    {
        // Identity
        [Required, MaxLength(100)]
        public string Username { get; set; }

        [MaxLength(150)]
        public string Email { get; set; }

        [MaxLength(15)]
        public string PhoneNumber { get; set; }

        // Security
        [Required]
        public string PasswordHash { get; set; }

        // Date/time the password was last set or reset (any of:
        // self-registration, self-service change, forgot-password reset,
        // admin create, admin reset). Null means never explicitly stamped
        // (e.g. a record created before this column existed).
        public DateTime? PasswordChangedOn { get; set; }

        public bool EmailConfirmed { get; set; }
        public bool PhoneConfirmed { get; set; }

        public int AccessFailedCount { get; set; }
        public DateTime? LockoutEnd { get; set; }
        public bool IsLocked { get; set; }

        // Multi-Tenant (CRITICAL 🔥)
        [Required]
        public string TenantId { get; set; }
        public virtual Tenant Tenant { get; set; }

        // Organization Mapping
        public string CompanyId { get; set; }
        public virtual Company Company { get; set; }

        public string? BranchId { get; set; }
        public virtual Branch Branch { get; set; }

        // Employee Link (Optional)
        public string? EmployeeId { get; set; }
        public virtual Employee Employee { get; set; }

        // Last Login Info
        public DateTime? LastLoginDate { get; set; }
        public string? LastLoginIP { get; set; }

        // Navigation
        public ICollection<UserRole> UserRoles { get; set; }
        public ICollection<RefreshToken> RefreshTokens { get; set; }

        public override string GetSequencePrefix() => "U";
    }
}
