using System.ComponentModel.DataAnnotations;
using System.Reflection;
using static APP.Helpers.EnumExtensions;

namespace APP.Models.DTOs
{
    public class EmployeeDto
    {
        // 🔹 Id (for update / detail)
        public string? Id { get; set; }

        [Display(Name ="First Name")]
        [Required, MaxLength(100)]
        public string FirstName { get; set; }

        [MaxLength(100)]
        [Display(Name = "Last Name")]
        public string? LastName { get; set; }

        [Required]
        [Display(Name = "Employee Code")]
        public string EmployeeCode { get; set; }

        // 🔹 Multi-Tenant
        [Required]
        [Display(Name = "Tenant")]
        public string TenantId { get; set; }

        [Required]
        [Display(Name = "Role")]
        public string RoleId { get; set; }

        public string? UserId { get; set; }

        // 🔹 Organization Mapping
        [Required]
        [Display(Name = "Company")]
        public string CompanyId { get; set; }

        [Display(Name = "Branch")]
        public string? BranchId { get; set; }

        [Display(Name = "Shift")]
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
        [MaxLength(150)]
        [EmailAddress]
        public string? Email { get; set; }

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
        [Display(Name = "PAN Number")]
        public string? PANNumber { get; set; }

        [RegularExpression(@"^\d{12}$", ErrorMessage = "Invalid Aadhar")]
        [Display(Name = "Aadhar Number")]
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
        public IFormFile? UploadImage { get; set; }
        public string? FilePath { get; set; }

        public string CreatedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public string? ModifiedBy { get; set; }

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

        public int Level { get; set; }

        public decimal? MinSalary { get; set; }
        public decimal? MaxSalary { get; set; }
    }

}
