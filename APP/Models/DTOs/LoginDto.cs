using System.ComponentModel.DataAnnotations;

namespace APP.Models.DTOs
{
    public class LoginDto
    {
        [Required(ErrorMessage = "User Name is required")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        [MinLength(6, ErrorMessage = "Minimum 6 characters required")]
        public string Password { get; set; }
        public string IpAddress { get; set; } = string.Empty;
        public bool RememberMe { get; set; } = false;

        // Populated server-side (from the request's User-Agent header) in
        // AuthController.Login just before posting to the API - not user
        // input, so no [Required]/validation attributes.
        public string? DeviceInfo { get; set; }
        public string? Browser { get; set; }
        public string? OS { get; set; }
    }

    public class ForgotPasswordDto
    {
        [Required(ErrorMessage = "Username is required")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Enter a valid email address")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Please enter a new password.")]
        [DataType(DataType.Password)]
        [MinLength(6, ErrorMessage = "New password must be at least 6 characters.")]
        [Display(Name = "New Password")]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "Please confirm your new password.")]
        [DataType(DataType.Password)]
        [Compare(nameof(NewPassword), ErrorMessage = "New password and confirmation do not match.")]
        [Display(Name = "Confirm New Password")]
        public string ConfirmPassword { get; set; }
    }

    public class ChangePasswordDto
    {
        [Required(ErrorMessage = "Please enter your current password.")]
        [DataType(DataType.Password)]
        [Display(Name = "Current Password")]
        public string OldPassword { get; set; }

        [Required(ErrorMessage = "Please enter a new password.")]
        [DataType(DataType.Password)]
        [MinLength(6, ErrorMessage = "New password must be at least 6 characters.")]
        [Display(Name = "New Password")]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "Please confirm your new password.")]
        [DataType(DataType.Password)]
        [Compare(nameof(NewPassword), ErrorMessage = "New password and confirmation do not match.")]
        [Display(Name = "Confirm New Password")]
        public string ConfirmPassword { get; set; }

        public string UserId { get; set; }
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
