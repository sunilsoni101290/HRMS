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
        public string? LastName { get; set; }

        // Multi-Tenant
        [Required]
        public string TenantId { get; set; }
        public virtual Tenant Tenant { get; set; }

        // Organization Mapping
        public string CompanyId { get; set; }
        public virtual Company Company { get; set; }

        public string? BranchId { get; set; }
        public virtual Branch Branch { get; set; }

        // Optional on the Add/Edit Employee form - only Employee Code,
        // First Name, Gender, Role and Company are required inputs.
        // See EF Core migration 20260913130000_MakeSomeEmployeeFieldsOptional
        // for the matching database change (nullable columns).
        public string? DepartmentId { get; set; }
        public virtual Department Department { get; set; }

        public string? DesignationId { get; set; }
        public virtual Designation Designation { get; set; }

        // Reporting Manager (Self Reference)
        public string? ReportingManagerId { get; set; }
        public virtual Employee ReportingManager { get; set; }

        // Personal Info
        public DateTime? DateOfBirth { get; set; }
        public Gender Gender { get; set; }
        public MaritalStatus MaritalStatus { get; set; } = MaritalStatus.Unmarried;

        // Contact Info
        [MaxLength(150)]
        public string? Email { get; set; }

        // Optional on the Add/Edit Employee form - see DepartmentId above.
        [MaxLength(15)]
        public string? Phone { get; set; }

        [MaxLength(15)]
        public string? EmergencyContact { get; set; }

        // Address - optional on the Add/Edit Employee form, see
        // DepartmentId above.
        public string? Address { get; set; }
        public string? Pincode { get; set; }

        // KYC (India Specific)
        [MaxLength(10)]
        public string? PANNumber { get; set; }

        [MaxLength(12)]
        public string? AadharNumber { get; set; }
        public string? FilePath { get; set; } // Image upload

        // Mandatory - see EF Core migration
        // 20260913120000_MakeEmployeeShiftMandatory (NOT NULL at the
        // database level too, matching CompanyId/DepartmentId/
        // DesignationId's existing pattern on this entity). A brand-new
        // Employee defaults to the Shift master's IsDefaultShift record
        // ("General Shift" - see DbSeeder.cs), resolved at the
        // controller/service layer, never hard-coded here.
        [Required]
        public string ShiftId { get; set; }
        public virtual Shift DefaultShift { get; set; }

        // Employment Details
        public DateTime JoiningDate { get; set; }
        public DateTime? ConfirmationDate { get; set; }
        public DateTime? RelievingDate { get; set; }

        // Probation end date - set/updated by ProbationConfirmationService
        // (via its own maker-checker workflow, never directly by
        // EmployeeService) once probation review begins for this employee.
        // Additive-only field; not yet populated automatically at
        // Employee-create time (nice-to-have for a future
        // EmployeeService.CreateAsync enhancement: JoiningDate +
        // Designation.ProbationPeriodMonths).
        public DateTime? ProbationEndDate { get; set; }

        #region Passport Details 
        public string? PassportNumber { get; set; }
        public DateTime? IssueDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string? PlaceOfIssue { get; set; }

        [Display(Name = "Nationality")]
        public Nationality Nationality { get; set; } = Nationality.Indian;

        [Display(Name = "Passport Status")]
        public PassportStatus PassportStatus { get; set; } = PassportStatus.NotAvailable;

        [Display(Name = "Passport Issuing Country")]
        public string? CountryId { get; set; }
        public Country? Country { get; set; }

        [Display(Name = "Passport Document")]
        public string? PassportFilePath { get; set; }
        #endregion

        public EmploymentType EmploymentType { get; set; }

        // Onboarding: links this Employee back to the Recruitment Candidate
        // they were created from, if any (null for employees added directly
        // by HR without going through Recruitment). Scalar-only, no
        // navigation property, to keep this addition minimal and avoid
        // destabilizing the existing heavily-used Employee entity/DTO chain.
        // Set by EmployeeService.CreateAsync from EmployeeDto.CandidateId,
        // which also triggers auto-creation of an OnboardingCase for the new
        // Employee - see IOnboardingService.CreateCaseAsync.
        public string? CandidateId { get; set; }

        public override string GetSequencePrefix() => "EMP";
        // Navigation
        public ICollection<EmployeeDocument> Documents { get; set; }

    }
}
