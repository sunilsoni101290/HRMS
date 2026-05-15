using System.ComponentModel.DataAnnotations;

namespace APP.Models.DTOs
{
    public class LoginDto
    {
        [Required(ErrorMessage = "User Name is required")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [MinLength(6, ErrorMessage = "Minimum 6 characters required")]
        public string Password { get; set; }

        public bool RememberMe { get; set; }
    }

    public class UserListDto
    {
        public string Id { get; set; }
        public string RoleId { get; set; }
        public string RoleName { get; set; }
        public string Designation { get; set; }
        // Identity
        public string Username { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }

        // Tenant / Organization
        public string TenantId { get; set; }
        public string? TenantName { get; set; }

        public string? CompanyId { get; set; }
        public string? CompanyName { get; set; }

        public string? BranchId { get; set; }
        public string? BranchName { get; set; }

        // Employee
        public string? EmployeeId { get; set; }
        public string? EmployeeName { get; set; }

        // Security / Status
        public bool EmailConfirmed { get; set; }
        public bool PhoneConfirmed { get; set; }

        public bool IsLocked { get; set; }
        public int AccessFailedCount { get; set; }

        // Login Info
        public DateTime? LastLoginDate { get; set; }
        public string? LastLoginIP { get; set; }

        // Common Status
        public bool IsActive { get; set; }

        // Audit
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }
    }
}
