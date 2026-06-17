using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using static Domain.Enums.EnumExtensions;

namespace Application.DTOs.Employee
{
    public class EmployeeDto
    {
        // 🔹 Id (for update / detail)
        public string? Id { get; set; }

        [Required, MaxLength(100)]
        [Display(Name ="First Name")]
        public string FirstName { get; set; }

        [MaxLength(100)]
        [Display(Name = "Last Name")]
        public string? LastName { get; set; }

        [Required]
        [Display(Name = "Employee Code")]
        public string EmployeeCode { get; set; }

        [Required]
        [Display(Name = "Role")]
        public string RoleId { get; set; }

        // 🔹 Multi-Tenant
        [Required]
        public string TenantId { get; set; }

        // 🔹 Organization Mapping
        [Required]
        [Display(Name = "Company")]
        public string CompanyId { get; set; }

        [Display(Name = "Branch")]
        public string? BranchId { get; set; }
        public string? ShiftId { get; set; }

        [Required]
        [Display(Name = "Department")]
        public string DepartmentId { get; set; }

        [Required]
        [Display(Name = "Designation")]
        public string DesignationId { get; set; }

        [Display(Name = "Reporting Manager")]
        public string? ReportingManagerId { get; set; }

        // 🔹 Personal Info
        [Display(Name = "DOB")]
        public DateTime? DateOfBirth { get; set; }

        [Required]
        public Gender Gender { get; set; }

        [Required]
        [Display(Name = "Marital Status")]
        public MaritalStatus MaritalStatus { get; set; }

        // 🔹 Contact Info
        [Required]
        [MaxLength(150)]
        [EmailAddress]
        public string Email { get; set; }

        [Required, MaxLength(15)]
        public string Phone { get; set; }

        [MaxLength(15)]
        [Display(Name = "Emergency Contact")]
        public string? EmergencyContact { get; set; }

        // 🔹 Address
        [Required]
        public string Address { get; set; }

        [Required]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "Invalid Pincode")]
        [Display(Name = "Pin Code")]
        public string Pincode { get; set; }

        // 🔹 KYC
        [RegularExpression(@"[A-Z]{5}[0-9]{4}[A-Z]{1}", ErrorMessage = "Invalid PAN")]
        [Display(Name = "PAN #")]
        public string? PANNumber { get; set; }

        [RegularExpression(@"^\d{12}$", ErrorMessage = "Invalid Aadhar")]
        [Display(Name = "Aadhar #")]
        public string? AadharNumber { get; set; }

        // 🔹 Employment
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

        [Display(Name = "Email Confirmed")]
        public bool EmailConfirmed { get; set; }

        [Display(Name = "Phone Confirmed")]
        public bool PhoneConfirmed { get; set; }

        // Branding
        [Display(Name = "Upload Profile Photo")]
        public string? FilePath { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public string? ModifiedBy { get; set; }

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
        public decimal? MinSalary { get; set; }
        public decimal? MaxSalary { get; set; }

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

        public decimal? MinSalary { get; set; }
        public decimal? MaxSalary { get; set; }
    }
}
