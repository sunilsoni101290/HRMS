using System.ComponentModel.DataAnnotations;

namespace APP.Models.DTOs
{
    // Mirrors Application.DTOs.Payroll.PayslipRequestDtos.cs exactly -
    // property names/types must match the API's JSON 1:1 or model binding
    // silently breaks. Backed by API/Controllers/PayslipRequestController.cs
    // (api/paysliprequest). Status is an int on the wire - see
    // Domain/Enums/EnumExtensions.PayslipRequestStatus for the values
    // (PendingManagerApproval=1, RejectedByManager=2, ApprovedByManager=3,
    // PendingFinanceAction=4, PayslipGenerated=5, Completed=6,
    // RejectedByFinance=7).
    public class PayslipRequestDto
    {
        public string? Id { get; set; }

        public string EmployeeId { get; set; }
        public string? EmployeeName { get; set; }
        public string? EmployeeCode { get; set; }
        public string? DepartmentName { get; set; }
        public string? ReportingManagerName { get; set; }

        public string PayrollId { get; set; }
        public int PayrollYear { get; set; }
        public int PayrollMonth { get; set; }

        public int Status { get; set; }
        public string? StatusName { get; set; }

        public string? ManagerRemarks { get; set; }
        public string? ManagerActionByName { get; set; }
        public DateTime? ManagerActionOn { get; set; }

        public string? FinanceRemarks { get; set; }
        public string? FinanceActionByName { get; set; }
        public DateTime? FinanceActionOn { get; set; }

        // Only populated by the API when Status == Completed.
        public string? DocumentUrl { get; set; }
        public string? DocumentFileName { get; set; }

        public string? GeneratedByName { get; set; }
        public DateTime? GeneratedOn { get; set; }

        public string? CompletedByName { get; set; }
        public DateTime? CompletedOn { get; set; }

        public string? TenantId { get; set; }
        public DateTime CreatedOn { get; set; }
        public string? CreatedBy { get; set; }

        public List<PayslipRequestAuditDto> Timeline { get; set; } = new();
    }

    public class PayslipRequestAuditDto
    {
        public string Action { get; set; }
        public string? PerformedByName { get; set; }
        public string? Remarks { get; set; }
        public DateTime PerformedOn { get; set; }
    }

    // Body for POST api/paysliprequest.
    public class CreatePayslipRequestDto
    {
        [Required]
        [Display(Name = "Employee")]
        public string EmployeeId { get; set; }

        [Required(ErrorMessage = "Please select a payroll period.")]
        [Display(Name = "Payroll Period")]
        public string PayrollId { get; set; }
    }

    // Body for PUT api/paysliprequest/{id}/manager-approve and
    // .../finance-complete - remarks are optional on both.
    public class PayslipRequestActionDto
    {
        [MaxLength(500)]
        public string? Remarks { get; set; }
    }

    // Body for PUT api/paysliprequest/{id}/manager-reject and
    // .../finance-reject - a reason is mandatory on both.
    public class PayslipRequestRejectDto
    {
        [Required(ErrorMessage = "Reason is required")]
        [MaxLength(500)]
        public string Reason { get; set; }
    }

    // Body for PUT api/paysliprequest/{id}/finance-upload - DocumentUrl is
    // the relative wwwroot path already saved to disk by
    // PayslipRequestController.FinanceUpload before this DTO is posted to
    // the API (mirrors EmployeeDocumentController.SaveFileAsync) - the API/
    // Application layer never touches an IFormFile directly.
    public class PayslipRequestUploadDto
    {
        [Required]
        public string DocumentUrl { get; set; }

        [Required]
        public string DocumentFileName { get; set; }

        [MaxLength(500)]
        public string? Remarks { get; set; }
    }

    // View model used purely by the Finance Upload Razor form - the actual
    // wire DTO sent to the API is PayslipRequestUploadDto, built server-side
    // in the controller after UploadFile is saved to disk.
    public class FinanceUploadViewModel
    {
        public string Id { get; set; }

        [Required(ErrorMessage = "Please choose the payslip PDF to upload.")]
        [Display(Name = "Payslip Document (PDF)")]
        public IFormFile? UploadFile { get; set; }

        [MaxLength(500)]
        public string? Remarks { get; set; }
    }
}
