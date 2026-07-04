using Domain.Enums;
using System.ComponentModel.DataAnnotations;
using static APP.Helpers.EnumExtensions;

namespace APP.Models.DTOs
{
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

        public string? ShiftName { get; set; }

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
        public string PassportNumber { get; set; }
        public DateTime IssueDate { get; set; }
        public DateTime ExpiryDate { get; set; }
        public string PlaceOfIssue { get; set; }

        [Display(Name = "Nationality")]
        public Nationality Nationality { get; set; } = Nationality.Indian;

        [Display(Name = "Passport Status")]
        public PassportStatus PassportStatus { get; set; } = PassportStatus.NotAvailable;

        [Display(Name = "Passport Issuing Country")]
        public string? CountryId { get; set; }
        public string? CountryName { get; set; }

        [Display(Name = "Upload Passport Document")]
        public IFormFile? UploadPassport { get; set; }

        [Display(Name = "Passport Document")]
        public string? PassportFilePath { get; set; }
        #endregion
    }

    public class EmployeeDropdownDto
    {
        public string Value { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }
    }
}
