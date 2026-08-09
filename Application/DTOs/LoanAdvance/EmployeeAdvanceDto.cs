using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.LoanAdvance
{
    /// <summary>Full read shape for Domain.Entities.EmployeeAdvance - mirror of EmployeeLoanDto minus interest/EMI fields.</summary>
    public class EmployeeAdvanceDto
    {
        public string Id { get; set; } = string.Empty;

        public string? TenantId { get; set; }
        public string? CompanyId { get; set; }
        public string? CompanyName { get; set; }
        public string? BranchId { get; set; }
        public string? BranchName { get; set; }

        public string EmployeeId { get; set; } = string.Empty;
        public string? EmployeeName { get; set; }
        public string? EmployeeCode { get; set; }
        public string? DepartmentName { get; set; }
        public string? DesignationName { get; set; }

        public string AdvanceTypeId { get; set; } = string.Empty;
        public string? AdvanceTypeName { get; set; }

        public decimal RequestedAmount { get; set; }
        public decimal? ApprovedAmount { get; set; }
        public int InstallmentCount { get; set; }
        public string? Purpose { get; set; }

        /// <summary>See Domain.Enums.EnumExtensions.AdvanceStatus.</summary>
        public int Status { get; set; }
        public string? StatusName { get; set; }

        public int CurrentApprovalLevel { get; set; }
        public int TotalApprovalLevels { get; set; }

        public string MakerId { get; set; } = string.Empty;
        public string? MakerName { get; set; }
        public DateTime MakerActionOn { get; set; }
        public string? MakerRemarks { get; set; }

        public decimal? DisbursedAmount { get; set; }
        public DateTime? DisbursedOn { get; set; }
        public int? DisbursementMode { get; set; }
        public string? DisbursementModeName { get; set; }
        public string? DisbursementReference { get; set; }

        public decimal OutstandingAmount { get; set; }
        public decimal TotalPaid { get; set; }
        public DateTime? NextDueDate { get; set; }
        public decimal? NextDueAmount { get; set; }

        public DateTime? ClosedOn { get; set; }

        public List<AdvanceApprovalHistoryDto> ApprovalHistory { get; set; } = new();
        public List<AdvanceInstallmentDto> Installments { get; set; } = new();
        public List<AdvancePaymentHistoryDto> PaymentHistory { get; set; } = new();
        public List<LoanAdvanceAttachmentDto> Attachments { get; set; } = new();

        public DateTime CreatedOn { get; set; }
        public string? CreatedBy { get; set; }
    }

    public class EmployeeAdvanceListDto
    {
        public string Id { get; set; } = string.Empty;
        public string EmployeeId { get; set; } = string.Empty;
        public string? EmployeeName { get; set; }
        public string? EmployeeCode { get; set; }
        public string? AdvanceTypeName { get; set; }
        public decimal RequestedAmount { get; set; }
        public decimal? ApprovedAmount { get; set; }
        public decimal OutstandingAmount { get; set; }
        public int Status { get; set; }
        public string? StatusName { get; set; }
        public int CurrentApprovalLevel { get; set; }
        public DateTime? DisbursedOn { get; set; }
        public DateTime CreatedOn { get; set; }
    }

    /// <summary>See LoanTypeDto's remarks for the client/server validation split.</summary>
    public class AdvanceSubmitDto
    {
        [Required(ErrorMessage = "Employee is required.")]
        public string EmployeeId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Advance Type is required.")]
        public string AdvanceTypeId { get; set; } = string.Empty;

        [Range(0.01, double.MaxValue, ErrorMessage = "Requested Amount must be greater than zero.")]
        public decimal RequestedAmount { get; set; }

        [Range(1, 60, ErrorMessage = "Installment Count must be between 1 and 60.")]
        public int InstallmentCount { get; set; } = 1;

        [StringLength(500)]
        public string? Purpose { get; set; }
    }

    public class AdvanceApprovalActionDto
    {
        [Required(ErrorMessage = "EmployeeAdvanceId is required.")]
        public string EmployeeAdvanceId { get; set; } = string.Empty;

        /// <summary>1=Approved, 2=Rejected.</summary>
        [Range(1, 2, ErrorMessage = "Decision must be 1 (Approved) or 2 (Rejected).")]
        public int Decision { get; set; }

        [StringLength(1000)]
        public string? Remarks { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Approved Amount must be greater than zero when specified.")]
        public decimal? ApprovedAmount { get; set; }
    }

    public class AdvanceDisbursementDto
    {
        [Required(ErrorMessage = "EmployeeAdvanceId is required.")]
        public string EmployeeAdvanceId { get; set; } = string.Empty;

        [Range(0.01, double.MaxValue, ErrorMessage = "Disbursed Amount must be greater than zero.")]
        public decimal DisbursedAmount { get; set; }

        [Required(ErrorMessage = "Disbursed On date is required.")]
        public DateTime DisbursedOn { get; set; }

        /// <summary>1=BankTransfer, 2=Cheque, 3=PayrollCredit.</summary>
        [Range(1, 3, ErrorMessage = "Disbursement Mode must be 1 (Bank Transfer), 2 (Cheque) or 3 (Payroll Credit).")]
        public int DisbursementMode { get; set; }

        [StringLength(100)]
        public string? DisbursementReference { get; set; }

        [Required(ErrorMessage = "First Installment Due Date is required.")]
        public DateTime FirstInstallmentDueDate { get; set; }
    }

    /// <summary>Manual settlement entry - marks any still-outstanding balance recovered outside the normal payroll cycle.</summary>
    public class AdvanceSettlementDto
    {
        [Required(ErrorMessage = "EmployeeAdvanceId is required.")]
        public string EmployeeAdvanceId { get; set; } = string.Empty;

        [Range(0.01, double.MaxValue, ErrorMessage = "Amount Paid must be greater than zero.")]
        public decimal AmountPaid { get; set; }

        [Required(ErrorMessage = "Payment Date is required.")]
        public DateTime PaymentDate { get; set; }

        [StringLength(100)]
        public string? ReceiptReference { get; set; }

        [StringLength(500)]
        public string? Remarks { get; set; }
    }
}
