using System.ComponentModel.DataAnnotations;

namespace APP.Models.DTOs
{
    // Mirrors Application.DTOs.LoanAdvance.EmployeeAdvanceDto exactly - see
    // LoanTypeDto's remarks. Backed by
    // API/Controllers/EmployeeAdvanceController.cs (api/employeeadvance).
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

        /// <summary>See EnumExtensions.AdvanceStatus.</summary>
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

    public class AdvanceApprovalHistoryDto
    {
        public string Id { get; set; } = string.Empty;
        public string EmployeeAdvanceId { get; set; } = string.Empty;
        public int LevelNumber { get; set; }
        public string CheckerId { get; set; } = string.Empty;
        public string? CheckerName { get; set; }
        public int Decision { get; set; }
        public string? DecisionName { get; set; }
        public string? Remarks { get; set; }
        public DateTime ActionOn { get; set; }
    }

    public class AdvanceInstallmentDto
    {
        public string Id { get; set; } = string.Empty;
        public string EmployeeAdvanceId { get; set; } = string.Empty;
        public int InstallmentNumber { get; set; }
        public DateTime DueDate { get; set; }
        public decimal InstallmentAmount { get; set; }
        public int Status { get; set; }
        public string? StatusName { get; set; }
        public DateTime? RecoveredOn { get; set; }
        public string? PayrollId { get; set; }
    }

    public class AdvancePaymentHistoryDto
    {
        public string Id { get; set; } = string.Empty;
        public string EmployeeAdvanceId { get; set; } = string.Empty;
        public string? AdvanceInstallmentId { get; set; }
        public int PaymentSource { get; set; }
        public string? PaymentSourceName { get; set; }
        public decimal AmountPaid { get; set; }
        public DateTime PaymentDate { get; set; }
        public string? PayrollId { get; set; }
        public string? ReceiptReference { get; set; }
        public string? Remarks { get; set; }
    }

    // ---------------- Request bodies ----------------

    public class AdvanceSubmitDto
    {
        [Required(ErrorMessage = "Employee is required.")]
        [Display(Name = "Employee")]
        public string EmployeeId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Advance Type is required.")]
        [Display(Name = "Advance Type")]
        public string AdvanceTypeId { get; set; } = string.Empty;

        [Range(0.01, double.MaxValue, ErrorMessage = "Requested Amount must be greater than zero.")]
        [Display(Name = "Requested Amount")]
        public decimal RequestedAmount { get; set; }

        [Range(1, 60, ErrorMessage = "Installment Count must be between 1 and 60.")]
        [Display(Name = "Installments")]
        public int InstallmentCount { get; set; } = 1;

        [StringLength(500)]
        public string? Purpose { get; set; }
    }

    public class AdvanceApprovalActionDto
    {
        [Required]
        public string EmployeeAdvanceId { get; set; } = string.Empty;

        [Range(1, 2)]
        public int Decision { get; set; }

        [StringLength(1000)]
        public string? Remarks { get; set; }

        public decimal? ApprovedAmount { get; set; }
    }

    public class AdvanceDisbursementDto
    {
        [Required]
        public string EmployeeAdvanceId { get; set; } = string.Empty;

        [Range(0.01, double.MaxValue, ErrorMessage = "Disbursed Amount must be greater than zero.")]
        [Display(Name = "Disbursed Amount")]
        public decimal DisbursedAmount { get; set; }

        [Required(ErrorMessage = "Disbursed On date is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "Disbursed On")]
        public DateTime DisbursedOn { get; set; } = DateTime.UtcNow.Date;

        [Range(1, 3)]
        [Display(Name = "Disbursement Mode")]
        public int DisbursementMode { get; set; } = 1;

        [StringLength(100)]
        [Display(Name = "Reference")]
        public string? DisbursementReference { get; set; }

        [Required(ErrorMessage = "First Installment Due Date is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "First Installment Due Date")]
        public DateTime FirstInstallmentDueDate { get; set; } = DateTime.UtcNow.Date.AddMonths(1);
    }

    public class AdvanceSettlementDto
    {
        [Required]
        public string EmployeeAdvanceId { get; set; } = string.Empty;

        [Range(0.01, double.MaxValue, ErrorMessage = "Amount Paid must be greater than zero.")]
        [Display(Name = "Amount Paid")]
        public decimal AmountPaid { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Payment Date")]
        public DateTime PaymentDate { get; set; } = DateTime.UtcNow.Date;

        [StringLength(100)]
        [Display(Name = "Receipt Reference")]
        public string? ReceiptReference { get; set; }

        [StringLength(500)]
        public string? Remarks { get; set; }
    }
}
