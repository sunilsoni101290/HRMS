using System.ComponentModel.DataAnnotations;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace APP.Models.Auth
{
    public class User
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

        public bool EmailConfirmed { get; set; }
        public bool PhoneConfirmed { get; set; }

        public int AccessFailedCount { get; set; }
        public DateTime? LockoutEnd { get; set; }
        public bool IsLocked { get; set; }

        // Multi-Tenant (CRITICAL 🔥)
        [Required]
        public string TenantId { get; set; }

        // Organization Mapping
        public string CompanyId { get; set; }

        public string? BranchId { get; set; }

        // Employee Link (Optional)
        public string? EmployeeId { get; set; }

        // Last Login Info
        public DateTime? LastLoginDate { get; set; }
        public string? LastLoginIP { get; set; }

    }
}
