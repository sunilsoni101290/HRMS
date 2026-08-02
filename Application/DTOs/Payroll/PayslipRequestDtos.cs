using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Payroll
{
    // ==============================
    // Payslip Request
    // ==============================

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

        public string? DocumentUrl { get; set; }
        public string? DocumentFileName { get; set; }

        public string? GeneratedByName { get; set; }
        public DateTime? GeneratedOn { get; set; }

        public string? CompletedByName { get; set; }
        public DateTime? CompletedOn { get; set; }

        public string? TenantId { get; set; }
        public DateTime CreatedOn { get; set; }
        public string? CreatedBy { get; set; }

        // Full who/when/remarks trail - see Domain/Entities/
        // PayslipRequestAudit.cs. Ordered oldest-first.
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

    // Body for PUT api/paysliprequest/{id}/finance-upload. DocumentUrl is
    // the relative wwwroot path already saved to disk by the APP controller
    // (see APP/Controllers/PayslipRequestController.cs FinanceUpload,
    // mirroring EmployeeDocumentController.SaveFileAsync) - the API/
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
}
