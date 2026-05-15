using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs.Auth
{
    public class LoginDto
    {
        public string Username { get; set; }
        public string Password { get; set; }
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
