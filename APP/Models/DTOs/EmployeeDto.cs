using System.ComponentModel.DataAnnotations;
using System.Reflection;
using static APP.Helpers.EnumExtensions;

namespace APP.Models.DTOs
{
    public class EmployeeDto
    {
        // 🔹 Id (for update / detail)
        public string? Id { get; set; }

        [Required, MaxLength(100)]
        public string FirstName { get; set; }

        [MaxLength(100)]
        public string? LastName { get; set; }

        // 🔹 Multi-Tenant
        [Required]
        public string TenantId { get; set; }

        // 🔹 Organization Mapping
        [Required]
        public string CompanyId { get; set; }

        public string? BranchId { get; set; }

        [Required]
        public string DepartmentId { get; set; }

        [Required]
        public string DesignationId { get; set; }

        public string? ReportingManagerId { get; set; }

        // 🔹 Personal Info
        public DateTime? DateOfBirth { get; set; }

        [Required]
        public Gender Gender { get; set; }

        [Required]
        public MaritalStatus MaritalStatus { get; set; }

        // 🔹 Contact Info
        [MaxLength(150)]
        [EmailAddress]
        public string? Email { get; set; }

        [Required, MaxLength(15)]
        public string Phone { get; set; }

        [MaxLength(15)]
        public string? EmergencyContact { get; set; }

        // 🔹 Address
        [Required]
        public string Address { get; set; }

        [Required]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "Invalid Pincode")]
        public string Pincode { get; set; }

        // 🔹 KYC
        [RegularExpression(@"[A-Z]{5}[0-9]{4}[A-Z]{1}", ErrorMessage = "Invalid PAN")]
        public string? PANNumber { get; set; }

        [RegularExpression(@"^\d{12}$", ErrorMessage = "Invalid Aadhar")]
        public string? AadharNumber { get; set; }

        // 🔹 Employment
        [Required]
        public DateTime JoiningDate { get; set; }

        public DateTime? ConfirmationDate { get; set; }
        public DateTime? RelievingDate { get; set; }

        [Required]
        public EmploymentType EmploymentType { get; set; }
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


    // ==============================
    // Country DTOs
    // ==============================

    public class CountryDto
    {
        public string? Id { get; set; }

        public string Name { get; set; }
        public string Code { get; set; }
        public string PhoneCode { get; set; }
    }

    public class CountryListDto
    {
        public string Id { get; set; }

        public string Name { get; set; }
        public string Code { get; set; }
        public string PhoneCode { get; set; }
    }


    // ==============================
    // State DTOs
    // ==============================

    public class StateDto
    {
        public string? Id { get; set; }

        public string Name { get; set; }
        public string GSTStateCode { get; set; }

        public string CountryId { get; set; }
    }

    public class StateListDto
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public string CountryId { get; set; }
        public string CountryName { get; set; }
    }


    // ==============================
    // City DTOs
    // ==============================

    public class CityDto
    {
        public string? Id { get; set; }

        public string Name { get; set; }

        public string StateId { get; set; }
    }

    public class CityListDto
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public string StateId { get; set; }
        public string StateName { get; set; }
    }
}
