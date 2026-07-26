using System.ComponentModel.DataAnnotations;

namespace APP.Models.DTOs
{
    // Mirrors Application.DTOs.EmployeeLifecycle.EmployeeTransferDto exactly -
    // property names/types must match the API's JSON 1:1 or model binding
    // silently breaks. Backed by API/Controllers/EmployeeTransferController.cs
    // (api/employeetransfer). Status is an int on the wire - see
    // EnumExtensions.TransferStatus for the values (PendingChecker=1,
    // Approved=2, Rejected=3). Phase 3 of the "Probation & Confirmation"
    // (Employee Lifecycle) module - Maker-Checker workflow.
    public class EmployeeTransferDto
    {
        public string Id { get; set; }

        public string EmployeeId { get; set; }
        public string? EmployeeName { get; set; }
        public string? EmployeeCode { get; set; }

        public DateTime EffectiveDate { get; set; }
        public string Reason { get; set; }

        // ---- From (snapshot) ----
        public string FromCompanyId { get; set; }
        public string? FromCompanyName { get; set; }

        public string? FromBranchId { get; set; }
        public string? FromBranchName { get; set; }

        public string FromDepartmentId { get; set; }
        public string? FromDepartmentName { get; set; }

        public string FromDesignationId { get; set; }
        public string? FromDesignationName { get; set; }

        public string? FromReportingManagerId { get; set; }
        public string? FromReportingManagerName { get; set; }

        // ---- To (proposed) ----
        public string? ToCompanyId { get; set; }
        public string? ToCompanyName { get; set; }

        public string? ToBranchId { get; set; }
        public string? ToBranchName { get; set; }

        public string? ToDepartmentId { get; set; }
        public string? ToDepartmentName { get; set; }

        public string? ToDesignationId { get; set; }
        public string? ToDesignationName { get; set; }

        public string? ToReportingManagerId { get; set; }
        public string? ToReportingManagerName { get; set; }

        // ---- Maker ----
        public string MakerId { get; set; }
        public string? MakerName { get; set; }
        public DateTime MakerActionOn { get; set; }
        public string? MakerRemarks { get; set; }

        public int Status { get; set; }
        public string? StatusName { get; set; }

        // ---- Checker ----
        public string? CheckerId { get; set; }
        public string? CheckerName { get; set; }
        public DateTime? CheckerActionOn { get; set; }
        public string? CheckerRemarks { get; set; }

        public string? TenantId { get; set; }
        public DateTime CreatedOn { get; set; }
        public string? CreatedBy { get; set; }
    }

    // Body for POST api/employeetransfer - mirrors
    // Application.DTOs.EmployeeLifecycle.CreateEmployeeTransferDto exactly.
    // All "To" fields are optional/nullable - the HR user only sets the
    // dimension(s) actually changing; the "From" snapshot is captured
    // automatically server-side, never supplied here. At least one "To"
    // field must be non-null and differ from the employee's current value -
    // validated server-side (a no-op transfer proposal is rejected with a
    // clear message).
    public class CreateEmployeeTransferDto
    {
        [Required]
        [Display(Name = "Employee")]
        public string EmployeeId { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime EffectiveDate { get; set; }

        [Required]
        public string Reason { get; set; }

        [Display(Name = "New Company")]
        public string? ToCompanyId { get; set; }

        [Display(Name = "New Branch")]
        public string? ToBranchId { get; set; }

        [Display(Name = "New Department")]
        public string? ToDepartmentId { get; set; }

        [Display(Name = "New Designation")]
        public string? ToDesignationId { get; set; }

        [Display(Name = "New Reporting Manager")]
        public string? ToReportingManagerId { get; set; }
    }
}
