using Domain.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using static Domain.Enums.EnumExtensions;

namespace Application.DTOs.Employee
{
    public class EmployeeDto : IValidatableObject
    {
        // Id
        public string? Id { get; set; }

        #region Basic Information

        [Required(ErrorMessage = "First Name is required.")]
        [Display(Name = "First Name")]
        [StringLength(100, MinimumLength = 2)]
        public string FirstName { get; set; }

        [Display(Name = "Last Name")]
        [StringLength(100)]
        public string? LastName { get; set; }

        [Required(ErrorMessage = "Employee Code is required.")]
        [Display(Name = "Employee Code")]
        [StringLength(20)]
        [RegularExpression(@"^[A-Za-z0-9_-]+$", ErrorMessage = "Employee Code can contain only letters, numbers, hyphen (-) and underscore (_).")]
        public string EmployeeCode { get; set; }

        [Required(ErrorMessage = "Please select Role.")]
        [Display(Name = "Role")]
        public string RoleId { get; set; }

        #endregion

        #region Multi Tenant

        [Required]
        public string TenantId { get; set; }

        [Required(ErrorMessage = "Please select Company.")]
        [Display(Name = "Company")]
        public string CompanyId { get; set; }

        [Display(Name = "Branch")]
        public string? BranchId { get; set; }

        [Display(Name = "Shift")]
        public string? ShiftId { get; set; }

        [Required(ErrorMessage = "Please select Department.")]
        [Display(Name = "Department")]
        public string DepartmentId { get; set; }

        [Required(ErrorMessage = "Please select Designation.")]
        [Display(Name = "Designation")]
        public string DesignationId { get; set; }

        [Display(Name = "Reporting Manager")]
        public string? ReportingManagerId { get; set; }

        #endregion

        #region Personal Information

        [Display(Name = "Date of Birth")]
        public DateTime? DateOfBirth { get; set; }

        [Required]
        public Gender Gender { get; set; }

        [Required]
        [Display(Name = "Marital Status")]
        public MaritalStatus MaritalStatus { get; set; }

        #endregion

        #region Contact Information

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid Email Address.")]
        [StringLength(150)]
        public string Email { get; set; }

        [Required(ErrorMessage = "Phone Number is required.")]
        [Display(Name = "Phone")]
        [RegularExpression(@"^[6-9]\d{9}$", ErrorMessage = "Invalid Mobile Number.")]
        public string Phone { get; set; }

        [Display(Name = "Emergency Contact")]
        [RegularExpression(@"^[6-9]\d{9}$", ErrorMessage = "Invalid Emergency Contact.")]
        public string? EmergencyContact { get; set; }

        #endregion

        #region Address

        [Required(ErrorMessage = "Address is required.")]
        [StringLength(500)]
        public string Address { get; set; }

        [Required]
        [Display(Name = "Pin Code")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "Invalid Pin Code.")]
        public string Pincode { get; set; }

        #endregion

        #region KYC

        [Display(Name = "PAN Number")]
        [RegularExpression(@"^[A-Z]{5}[0-9]{4}[A-Z]{1}$", ErrorMessage = "Invalid PAN Number.")]
        public string? PANNumber { get; set; }

        [Display(Name = "Aadhaar Number")]
        [RegularExpression(@"^\d{12}$", ErrorMessage = "Invalid Aadhaar Number.")]
        public string? AadharNumber { get; set; }

        #endregion

        #region Employment

        [Required]
        [Display(Name = "Joining Date")]
        public DateTime JoiningDate { get; set; }

        [Display(Name = "Confirmation Date")]
        public DateTime? ConfirmationDate { get; set; }

        [Display(Name = "Relieving Date")]
        public DateTime? RelievingDate { get; set; }

        [Required]
        [Display(Name = "Employment Type")]
        public EmploymentType EmploymentType { get; set; }

        #endregion

        #region Verification

        [Display(Name = "Email Confirmed")]
        public bool EmailConfirmed { get; set; }

        [Display(Name = "Phone Confirmed")]
        public bool PhoneConfirmed { get; set; }

        #endregion

        #region Profile

        [Display(Name = "Profile Photo")]
        public string? FilePath { get; set; }

        #endregion

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

        [Display(Name = "Passport Document")]
        public string? PassportFilePath { get; set; }
        #endregion

        #region Audit

        [Required]
        public string CreatedBy { get; set; }

        public DateTime? ModifiedOn { get; set; }

        public string? ModifiedBy { get; set; }

        #endregion

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (DateOfBirth.HasValue &&
                DateOfBirth.Value.Date > DateTime.Today)
            {
                yield return new ValidationResult(
                    "Date of Birth cannot be in the future.",
                    new[] { nameof(DateOfBirth) });
            }

            if (DateOfBirth.HasValue &&
                JoiningDate <= DateOfBirth.Value)
            {
                yield return new ValidationResult(
                    "Joining Date must be after Date of Birth.",
                    new[] { nameof(JoiningDate) });
            }

            if (ConfirmationDate.HasValue &&
                ConfirmationDate < JoiningDate)
            {
                yield return new ValidationResult(
                    "Confirmation Date cannot be before Joining Date.",
                    new[] { nameof(ConfirmationDate) });
            }

            if (RelievingDate.HasValue &&
                RelievingDate < JoiningDate)
            {
                yield return new ValidationResult(
                    "Relieving Date cannot be before Joining Date.",
                    new[] { nameof(RelievingDate) });
            }


            if (!string.IsNullOrWhiteSpace(Id) &&
                ReportingManagerId == Id)
            {
                yield return new ValidationResult(
                    "Employee cannot report to themselves.",
                    new[] { nameof(ReportingManagerId) });
            }
        }
    }

    public class EmployeeHierarchyDto
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Designation { get; set; }

        public List<EmployeeHierarchyDto> Children { get; set; } = new();
    }

    public class EmployeeSearchRequest
    {
        public string? SearchText { get; set; }

        public string? DepartmentId { get; set; }
        public string? DesignationId { get; set; }
        public string? CompanyId { get; set; }

        public bool? IsActive { get; set; }

        // Pagination
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;

        // Sorting
        public string? SortBy { get; set; } = "CreatedOn";
        public string? SortDirection { get; set; } = "desc";
    }

    public class EmployeeListDto
    {
        public string Id { get; set; }

        public string EmployeeCode { get; set; }

        public string FirstName { get; set; }

        public string? LastName { get; set; }

        // Tenant & Company
        public string TenantId { get; set; }

        public string CompanyId { get; set; }

        public string? CompanyName { get; set; }

        // Branch
        public string? BranchId { get; set; }

        public string? BrnachName { get; set; }

        // Department
        public string? DepartmentId { get; set; }

        public string? DepartmentName { get; set; }

        // Designation
        public string? DesignationId { get; set; }

        public string? DesignationName { get; set; }

        // Shift
        public string? ShiftId { get; set; }

        // RoleId
        public string? RoleId { get; set; }

        public string? shiftName { get; set; }

        // Reporting Manager
        public string? ReportingManagerId { get; set; }

        public string? ReportingManagerName { get; set; }

        // Personal Details
        public DateTime? DateOfBirth { get; set; }

        public string? Gender { get; set; }

        public string? MaritalStatus { get; set; }

        // Contact Details
        public string? Email { get; set; }

        public string? Phone { get; set; }

        public string? EmergencyContact { get; set; }

        public bool EmailConfirmed { get; set; }

        public bool PhoneConfirmed { get; set; }

        // Address
        public string? Address { get; set; }

        public string? Pincode { get; set; }

        // Documents
        public string? PANNumber { get; set; }

        public string? AadharNumber { get; set; }

        // Employment Details
        public DateTime? JoiningDate { get; set; }

        public DateTime? ConfirmationDate { get; set; }

        public DateTime? RelievingDate { get; set; }

        public string? EmploymentType { get; set; }
        public string? FilePath { get; set; }

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
        public string? CountryName { get; set; }

        [Display(Name = "Passport Document")]
        public string? PassportFilePath { get; set; }
        #endregion
    }

    public class PagedResult<T>
    {
        public int TotalRecords { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }

        public List<T> Data { get; set; }
    }

 // ==============================
// Department DTOs
// ==============================

    public class DepartmentDto
    {
        public string? Id { get; set; }

        public string Name { get; set; }
        public string Code { get; set; }

        // Multi Tenant
        public string TenantId { get; set; }

        public string? CompanyId { get; set; }
        public string? BranchId { get; set; }

        public string CreatedBy { get; set; }

        public DateTime? ModifiedOn { get; set; }
        public string? ModifiedBy { get; set; }

        // Hierarchy
        public string? ParentDepartmentId { get; set; }
    }

    public class DepartmentListDto
    {
        public string Id { get; set; }

        public string Name { get; set; }
        public string Code { get; set; }

        public string? CompanyName { get; set; }
        public string? CompanyId { get; set; }
        public string? BranchName { get; set; }
        public string? BranchId { get; set; }

        public string CreatedBy { get; set; }

        public DateTime? ModifiedOn { get; set; }
        public string? ModifiedBy { get; set; }

        public string? ParentDepartmentName { get; set; }
        public string? ParentDepartmentId { get; set; }
    }


    // ==============================
    // Designation DTOs
    // ==============================

    public class DesignationDto
    {
        public string? Id { get; set; }

        public string Name { get; set; }
        public string Code { get; set; }

        // Multi Tenant
        public string TenantId { get; set; }

        public string? CompanyId { get; set; }
        public string? BranchId { get; set; }

        // Department
        public string DepartmentId { get; set; }

        // Hierarchy
        public string? ParentDesignationId { get; set; }

        // Level
        public int Level { get; set; }

        // Salary
        public decimal MinSalary { get; set; } = 0.00m;
        public decimal MaxSalary { get; set; } = 0.00m;

        public string CreatedBy { get; set; }

        public DateTime? ModifiedOn { get; set; }
        public string? ModifiedBy { get; set; }

    }

    public class DesignationListDto
    {
        public string Id { get; set; }

        public string Name { get; set; }
        public string Code { get; set; }

        public string? DepartmentName { get; set; }
        public string? DepartmentId { get; set; }

        public string? CompanyName { get; set; }
        public string? CompanyId { get; set; }
        public string? BranchName { get; set; }
        public string? BranchId { get; set; }

        public string? ParentDesignationName { get; set; }
        public string? ParentDesignationId { get; set; }
        public int Level { get; set; }

        public decimal MinSalary { get; set; } = 0.00m;
        public decimal MaxSalary { get; set; } = 0.00m;
    }
}
