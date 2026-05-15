using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Application.DTOs.Auth
{
    public class RegisterDto
    {
        // Basic Info
        [Required]
        [MaxLength(100)]
        public string Username { get; set; }

        [EmailAddress]
        [MaxLength(150)]
        public string Email { get; set; }

        [Phone]
        [MaxLength(15)]
        public string PhoneNumber { get; set; }

        // Password (Plain → Hash later)
        [Required]
        [MinLength(6)]
        public string Password { get; set; }

        [Required]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; }

        // Multi-Tenant (🔥 Important)
        [Required]
        public string TenantId { get; set; }

        // Organization Mapping
        public string CompanyId { get; set; }
        public string RoleId { get; set; }
        public string? BranchId { get; set; }

        // Optional Employee Link
        public string? EmployeeId { get; set; }
        public string CreatedBy { get; set; }
    }
}
