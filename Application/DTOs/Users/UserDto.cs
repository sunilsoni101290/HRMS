using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Users
{
    public class UserDto
    {
        public string? Id { get; set; }

        [Required(ErrorMessage = "Username is required.")]
        [MaxLength(100)]
        [Display(Name = "Username")]
        public string Username { get; set; }

        [EmailAddress(ErrorMessage = "Enter a valid email.")]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        [Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; }

        // Only used on Create
        [Display(Name = "Password")]
        public string? Password { get; set; }

        [Required(ErrorMessage = "At least one role is required.")]
        [Display(Name = "Roles")]
        public List<string> RoleIds { get; set; } = new();
        public string? RoleNames { get; set; }

        [Display(Name = "Company")]
        public string? CompanyId { get; set; }

        [Display(Name = "Branch")]
        public string? BranchId { get; set; }

        [Display(Name = "Employee")]
        public string? EmployeeId { get; set; }
        public string? EmployeeName { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Locked")]
        public bool IsLocked { get; set; }

        public string? TenantId { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public string? ModifiedBy { get; set; }
    }

    public class ResetPasswordRequest
    {
        public string UserId { get; set; }
        public string NewPassword { get; set; }
    }
}
