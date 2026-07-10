using Domain.Enums;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using static APP.Helpers.EnumExtensions;

namespace APP.Models.DTOs
{
    public class EmployeeDto : IValidatableObject
    {
        // Id
        public string? Id { get; set; }

        #region Basic Information

        [Required(ErrorMessage = "First Name is required.")]
        [Display(Name = "First Name")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "First Name must be between 2 and 100 characters.")]
        public string FirstName { get; set; }

        [Display(Name = "Last Name")]
        [StringLength(100, ErrorMessage = "Last Name cannot exceed 100 characters.")]
        public string? LastName { get; set; }

        [Required(ErrorMessage = "Employee Code is required.")]
        [Display(Name = "Employee Code")]
        [StringLength(20, ErrorMessage = "Employee Code cannot exceed 20 characters.")]
        [RegularExpression(@"^[A-Za-z0-9_-]+$", ErrorMessage = "Employee Code can contain only letters, numbers, hyphen (-) and underscore (_).")]
        public string EmployeeCode { get; set; }

        #endregion

        #region Multi Tenant

        [Required]
        [Display(Name = "Tenant")]
        public string TenantId { get; set; }

        [Required(ErrorMessage = "Please select Role.")]
        [Display(Name = "Role")]
        public string RoleId { get; set; }

        public string? UserId { get; set; }

        #endregion

        #region Organization

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

        [DataType(DataType.Date)]
        [Display(Name = "Date of Birth")]
        public DateTime? DateOfBirth { get; set; }

        [Required(ErrorMessage = "Please select Gender.")]
        public Gender Gender { get; set; }

        [Required(ErrorMessage = "Please select Marital Status.")]
        [Display(Name = "Marital Status")]
        public MaritalStatus MaritalStatus { get; set; }

        #endregion

        #region Contact Information

        [Display(Name = "Email")]
        [StringLength(150)]
        [EmailAddress(ErrorMessage = "Invalid Email Address.")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Phone Number is required.")]
        [Display(Name = "Phone Number")]
        [RegularExpression(@"^[6-9]\d{9}$", ErrorMessage = "Invalid Mobile Number.")]
        public string Phone { get; set; }

        [Display(Name = "Emergency Contact")]
        [RegularExpression(@"^[6-9]\d{9}$", ErrorMessage = "Invalid Emergency Contact Number.")]
        public string? EmergencyContact { get; set; }

        #endregion

        #region Address

        [Required(ErrorMessage = "Address is required.")]
        [StringLength(500)]
        public string Address { get; set; }

        [Required(ErrorMessage = "Pin Code is required.")]
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

        [Required(ErrorMessage = "Joining Date is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "Joining Date")]
        public DateTime JoiningDate { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Confirmation Date")]
        public DateTime? ConfirmationDate { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Relieving Date")]
        public DateTime? RelievingDate { get; set; }

        [Required(ErrorMessage = "Employment Type is required.")]
        [Display(Name = "Employment Type")]
        public EmploymentType EmploymentType { get; set; }

        [Display(Name = "Email Confirmed")]
        public bool EmailConfirmed { get; set; }

        [Display(Name = "Phone Confirmed")]
        public bool PhoneConfirmed { get; set; }

        #endregion

        #region Profile Photo

        [Display(Name = "Profile Photo")]
        public IFormFile? UploadImage { get; set; }

        public string? FilePath { get; set; }

        #endregion

        #region Passport Details 
        [Display(Name = "Passport #")]
        public string? PassportNumber { get; set; }

        [Display(Name = "Issue Date")]
        [DataType(DataType.Date)]
        public DateTime IssueDate { get; set; }

        [Display(Name = "Expiry Date")]
        [DataType(DataType.Date)]
        public DateTime ExpiryDate { get; set; }

        [Display(Name = "Place Of Issue")]
        public string? PlaceOfIssue { get; set; }

        [Display(Name = "Nationality")]
        public Nationality Nationality { get; set; } = Nationality.Indian;

        [Display(Name = "Passport Status")]
        public PassportStatus PassportStatus { get; set; } = PassportStatus.NotAvailable;

        [Display(Name = "Passport Issuing Country")]
        public string? CountryId { get; set; }

        [Display(Name = "Upload Passport Document")]
        public IFormFile? UploadPassport { get; set; }

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
            // DOB Validation
            if (DateOfBirth.HasValue)
            {
                if (DateOfBirth.Value.Date > DateTime.Today)
                {
                    yield return new ValidationResult(
                        "Date of Birth cannot be in the future.",
                        new[] { nameof(DateOfBirth) });
                }

                int age = DateTime.Today.Year - DateOfBirth.Value.Year;

                if (DateOfBirth.Value.Date > DateTime.Today.AddYears(-age))
                    age--;

                if (age < 18)
                {
                    yield return new ValidationResult(
                        "Employee must be at least 18 years old.",
                        new[] { nameof(DateOfBirth) });
                }

                if (JoiningDate <= DateOfBirth.Value)
                {
                    yield return new ValidationResult(
                        "Joining Date must be after Date of Birth.",
                        new[] { nameof(JoiningDate) });
                }
            }

            // Confirmation Date
            if (ConfirmationDate.HasValue &&
                ConfirmationDate.Value < JoiningDate)
            {
                yield return new ValidationResult(
                    "Confirmation Date cannot be before Joining Date.",
                    new[] { nameof(ConfirmationDate) });
            }

            // Relieving Date
            if (RelievingDate.HasValue &&
                RelievingDate.Value < JoiningDate)
            {
                yield return new ValidationResult(
                    "Relieving Date cannot be before Joining Date.",
                    new[] { nameof(RelievingDate) });
            }

            // Emergency Contact
            if (!string.IsNullOrWhiteSpace(EmergencyContact) &&
                EmergencyContact == Phone)
            {
                yield return new ValidationResult(
                    "Emergency Contact cannot be the same as Phone Number.",
                    new[] { nameof(EmergencyContact) });
            }

            // Reporting Manager
            if (!string.IsNullOrWhiteSpace(Id) &&
                ReportingManagerId == Id)
            {
                yield return new ValidationResult(
                    "Employee cannot report to themselves.",
                    new[] { nameof(ReportingManagerId) });
            }
        }
    }

    // ==============================
    // Employee Import DTOs
    // ==============================

    public class EmployeeImportRowResult
    {
        public int RowNumber { get; set; }
        public string? EmployeeCode { get; set; }
        public string? EmployeeName { get; set; }
        public bool Success { get; set; }
        public string? Message { get; set; }
    }

    public class EmployeeImportResultDto
    {
        public int TotalRows { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public List<EmployeeImportRowResult> Rows { get; set; } = new();
    }

    // ==============================
    // Department DTOs
    // ==============================

    public class DepartmentDto : IValidatableObject
    {
        public string? Id { get; set; }

        [Required(ErrorMessage = "Department Name is required.")]
        [Display(Name = "Department Name")]
        [StringLength(100, MinimumLength = 2,
            ErrorMessage = "Department Name must be between 2 and 100 characters.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Department Code is required.")]
        [Display(Name = "Department Code")]
        [StringLength(20,
            ErrorMessage = "Department Code cannot exceed 20 characters.")]
        [RegularExpression(@"^[A-Za-z0-9_-]+$",
            ErrorMessage = "Department Code can contain only letters, numbers, hyphen (-) and underscore (_).")]
        public string Code { get; set; }

        // Multi Tenant
        [Required]
        public string TenantId { get; set; }

        [Display(Name = "Company")]
        public string? CompanyId { get; set; }

        [Display(Name = "Branch")]
        public string? BranchId { get; set; }

        [Required]
        public string CreatedBy { get; set; }

        public DateTime? ModifiedOn { get; set; }

        public string? ModifiedBy { get; set; }

        // Hierarchy
        [Display(Name = "Parent Department")]
        public string? ParentDepartmentId { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!string.IsNullOrWhiteSpace(Id) &&
                !string.IsNullOrWhiteSpace(ParentDepartmentId) &&
                Id == ParentDepartmentId)
            {
                yield return new ValidationResult(
                    "Department cannot be its own parent.",
                    new[] { nameof(ParentDepartmentId) });
            }
        }
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

        [Required(ErrorMessage = "Designation Name is required.")]
        [Display(Name = "Designation Name")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Designation Name must be between 2 and 100 characters.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Designation Code is required.")]
        [Display(Name = "Designation Code")]
        [StringLength(20, ErrorMessage = "Designation Code cannot exceed 20 characters.")]
        [RegularExpression(@"^[A-Za-z0-9_-]+$", ErrorMessage = "Designation Code can contain only letters, numbers, hyphen (-) and underscore (_).")]
        public string Code { get; set; }

        // Multi Tenant
        [Required]
        public string TenantId { get; set; }

        [Display(Name = "Company")]
        public string? CompanyId { get; set; }

        [Display(Name = "Branch")]
        public string? BranchId { get; set; }

        // Department
        [Required(ErrorMessage = "Please select Department.")]
        [Display(Name = "Department")]
        public string DepartmentId { get; set; }

        // Hierarchy
        [Display(Name = "Parent Designation")]
        public string? ParentDesignationId { get; set; }

        // Level
        [Required(ErrorMessage = "Designation Level is required.")]
        [Display(Name = "Level")]
        [Range(1, 100, ErrorMessage = "Level must be between 1 and 100.")]
        public int Level { get; set; }

        // Salary
        [Display(Name = "Minimum Salary")]
        [Range(0, 99999999.99, ErrorMessage = "Minimum Salary must be greater than or equal to 0.")]
        public decimal MinSalary { get; set; }

        [Display(Name = "Maximum Salary")]
        [Range(0, 99999999.99, ErrorMessage = "Maximum Salary must be greater than or equal to 0.")]
        public decimal MaxSalary { get; set; }

        [Required]
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
