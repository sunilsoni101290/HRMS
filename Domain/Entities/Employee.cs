using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using static Domain.Enums.EnumExtensions;

namespace Domain.Entities
{
    public class Employee : BaseEntity
    {
        // Basic Info
        [Required, MaxLength(50)]
        public string EmployeeCode { get; set; }

        [Required, MaxLength(100)]
        public string FirstName { get; set; }

        [MaxLength(100)]
        public string LastName { get; set; }

        // Multi-Tenant
        [Required]
        public string TenantId { get; set; }
        public virtual Tenant Tenant { get; set; }

        // Organization Mapping
        public string CompanyId { get; set; }
        public virtual Company Company { get; set; }

        public string? BranchId { get; set; }
        public virtual Branch Branch { get; set; }

        public string DepartmentId { get; set; }
        public virtual Department Department { get; set; }

        public string DesignationId { get; set; }
        public virtual Designation Designation { get; set; }

        // Reporting Manager (Self Reference)
        public string? ReportingManagerId { get; set; }
        public virtual Employee ReportingManager { get; set; }

        // Personal Info
        public DateTime? DateOfBirth { get; set; }
        public Gender Gender { get; set; }
        public MaritalStatus MaritalStatus { get; set; }

        // Contact Info
        [MaxLength(150)]
        public string? Email { get; set; }

        [MaxLength(15)]
        public string Phone { get; set; }

        [MaxLength(15)]
        public string? EmergencyContact { get; set; }

        // Address
        public string Address { get; set; }
        public string Pincode { get; set; }

        // KYC (India Specific)
        [MaxLength(10)]
        public string? PANNumber { get; set; }

        [MaxLength(12)]
        public string? AadharNumber { get; set; }

        // Employment Details
        public DateTime JoiningDate { get; set; }
        public DateTime? ConfirmationDate { get; set; }
        public DateTime? RelievingDate { get; set; }

        public EmploymentType EmploymentType { get; set; }
        public override string GetSequencePrefix() => "EMP";
        // Navigation
        public ICollection<EmployeeDocument> Documents { get; set; }

    }
}
