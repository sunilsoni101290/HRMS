using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs.Auth
{
    public class LoginDto
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string IpAddress { get; set; }

        // Populated by the APP layer from the HTTP request (User-Agent
        // header) before this DTO is posted to the API - purely
        // informational, used only to enrich the LoginHistory row.
        public string? DeviceInfo { get; set; }
        public string? Browser { get; set; }
        public string? OS { get; set; }
    }

    public class ChangePAsswordDto
    {
        // Kept for backward compatibility with any older caller, but the API
        // ignores this and always resolves the target user from the caller's
        // own JWT claims - a user can only ever change their own password.
        public string UserId { get; set; }
        public string OldPassword { get; set; }
        public string NewPassword { get; set; }
    }

    public class ChangePasswordResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
    }

    // Self-service password reset with no email/SMS infrastructure in this
    // system yet: identity is proven by matching BOTH the Username and the
    // Email already on file for that account (rather than a raw user id,
    // which would let anyone reset any account just by guessing an id).
    public class ForgotPasswordDto
    {
        public string Username { get; set; }
        public string Email { get; set; }
        public string NewPassword { get; set; }
    }

    public class ForgotPasswordResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
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
