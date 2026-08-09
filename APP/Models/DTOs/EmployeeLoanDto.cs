using System.ComponentModel.DataAnnotations;

namespace APP.Models.DTOs
{
    // Mirrors Application.DTOs.LoanAdvance.EmployeeLoanDto exactly - see
    // LoanTypeDto's remarks. Backed by
    // API/Controllers/EmployeeLoanController.cs (api/employeeloan).
    public class EmployeeLoanDto
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

        public string LoanTypeId { get; set; } = string.Empty;
        public string? LoanTypeName { get; set; }

        public string LoanPolicyId { get; set; } = string.Empty;

        public decimal RequestedAmount { get; set; }
        public decimal? ApprovedAmount { get; set; }

        public int TenureMonths { get; set; }
        public decimal InterestRatePercent { get; set; }

        public int InterestMethod { get; set; }
        public string? InterestMethodName { get; set; }

        public string? Purpose { get; set; }

        /// <summary>See EnumExtensions.LoanStatus.</summary>
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

        public decimal OutstandingPrincipal { get; set; }
        public decimal TotalPrincipalPaid { get; set; }
        public decimal TotalInterestPaid { get; set; }
        public DateTime? NextDueDate { get; set; }
        public decimal? NextDueAmount { get; set; }
        public int DaysPastDue { get; set; }

        public DateTime? ClosedOn { get; set; }
        public int? ClosureReason { get; set; }
        public string? ClosureReasonName { get; set; }

        public List<LoanApprovalHistoryDto> ApprovalHistory { get; set; } = new();
        public List<LoanEmiScheduleDto> EmiSchedule { get; set; } = new();
        public List<LoanPaymentHistoryDto> PaymentHistory { get; set; } = new();
        public List<LoanAdvanceAttachmentDto> Attachments { get; set; } = new();

        public DateTime CreatedOn { get; set; }
        public string? CreatedBy { get; set; }
    }

    public class EmployeeLoanListDto
    {
        public string Id { get; set; } = string.Empty;
        public string EmployeeId { get; set; } = string.Empty;
        public string? EmployeeName { get; set; }
        public string? EmployeeCode { get; set; }
        public string? LoanTypeName { get; set; }
        public decimal RequestedAmount { get; set; }
        public decimal? ApprovedAmount { get; set; }
        public decimal OutstandingPrincipal { get; set; }
        public int Status { get; set; }
        public string? StatusName { get; set; }
        public int CurrentApprovalLevel { get; set; }
        public DateTime? DisbursedOn { get; set; }
        public DateTime CreatedOn { get; set; }
    }

    public class LoanApprovalHistoryDto
    {
        public string Id { get; set; } = string.Empty;
        public string EmployeeLoanId { get; set; } = string.Empty;
        public int LevelNumber { get; set; }
        public string CheckerId { get; set; } = string.Empty;
        public string? CheckerName { get; set; }
        public int Decision { get; set; }
        public string? DecisionName { get; set; }
        public string? Remarks { get; set; }
        public DateTime ActionOn { get; set; }
    }

    public class LoanEmiScheduleDto
    {
        public string Id { get; set; } = string.Empty;
        public string EmployeeLoanId { get; set; } = string.Empty;
        public int InstallmentNumber { get; set; }
        public DateTime DueDate { get; set; }
        public decimal OpeningBalance { get; set; }
        public decimal PrincipalComponent { get; set; }
        public decimal InterestComponent { get; set; }
        public decimal EmiAmount { get; set; }
        public decimal ClosingBalance { get; set; }
        public int Status { get; set; }
        public string? StatusName { get; set; }
        public DateTime? RecoveredOn { get; set; }
        public string? PayrollId { get; set; }
    }

    public class LoanPaymentHistoryDto
    {
        public string Id { get; set; } = string.Empty;
        public string EmployeeLoanId { get; set; } = string.Empty;
        public string? LoanEmiScheduleId { get; set; }
        public int PaymentSource { get; set; }
        public string? PaymentSourceName { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal PrincipalPaid { get; set; }
        public decimal InterestPaid { get; set; }
        public DateTime PaymentDate { get; set; }
        public string? PayrollId { get; set; }
        public string? ReceiptReference { get; set; }
        public string? Remarks { get; set; }
    }

    public class LoanAdvanceAttachmentDto
    {
        public string Id { get; set; } = string.Empty;
        public int EntityType { get; set; }
        public string EntityId { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public string UploadedBy { get; set; } = string.Empty;
        public string? UploadedByName { get; set; }
        public DateTime CreatedOn { get; set; }
    }

    // ---------------- Request bodies ----------------

    public class LoanSubmitDto
    {
        [Required(ErrorMessage = "Employee is required.")]
        [Display(Name = "Employee")]
        public string EmployeeId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Loan Type is required.")]
        [Display(Name = "Loan Type")]
        public string LoanTypeId { get; set; } = string.Empty;

        [Range(0.01, double.MaxValue, ErrorMessage = "Requested Amount must be greater than zero.")]
        [Display(Name = "Requested Amount")]
        public decimal RequestedAmount { get; set; }

        [Range(1, 360, ErrorMessage = "Tenure Months must be between 1 and 360.")]
        [Display(Name = "Tenure (Months)")]
        public int TenureMonths { get; set; }

        [StringLength(500)]
        public string? Purpose { get; set; }
        public string? TenantId { get; set; }
        public string? ActingUserId { get; set; }
    }

    public class LoanApprovalActionDto
    {
        [Required]
        public string EmployeeLoanId { get; set; } = string.Empty;

        [Range(1, 2)]
        public int Decision { get; set; }

        [StringLength(1000)]
        public string? Remarks { get; set; }

        public decimal? ApprovedAmount { get; set; }
        public string? TenantId { get; set; }
        public string? ActingUserId { get; set; }
    }

    public class LoanDisbursementDto
    {
        [Required]
        public string EmployeeLoanId { get; set; } = string.Empty;

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

        [Required(ErrorMessage = "First EMI Due Date is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "First EMI Due Date")]
        public DateTime FirstEmiDueDate { get; set; } = DateTime.UtcNow.Date.AddMonths(1);
        public string? TenantId { get; set; }
        public string? ActingUserId { get; set; }
    }

    public class LoanPreClosureQuoteDto
    {
        public string EmployeeLoanId { get; set; } = string.Empty;
        public DateTime AsOfDate { get; set; }
        public decimal OutstandingPrincipal { get; set; }
        public decimal AccruedInterest { get; set; }
        public decimal PreClosurePenalty { get; set; }
        public decimal TotalPayable { get; set; }
    }

    public class LoanSettlementDto
    {
        [Required]
        public string EmployeeLoanId { get; set; } = string.Empty;

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

        [Range(2, 4)]
        [Display(Name = "Closure Reason")]
        public int ClosureReason { get; set; } = 2;
        public string? TenantId { get; set; }
        public string? ActingUserId { get; set; }

    }

    public class LoanEligibilityDto
    {
        public bool IsEligible { get; set; }
        public string? ReasonIfNotEligible { get; set; }
        public decimal MaxEligibleAmount { get; set; }
        public int MaxEligibleTenureMonths { get; set; }
        public decimal CurrentTotalOutstanding { get; set; }
        public int CurrentActiveLoanCount { get; set; }
        public decimal MonthlyGrossSalary { get; set; }
        public decimal MonthlyNetSalary { get; set; }
        public decimal ExistingMonthlyDeductions { get; set; }
        public decimal MaxAdditionalMonthlyDeduction { get; set; }
    }

    public class EmiPreviewRequestDto
    {
        public string LoanTypeId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public int TenureMonths { get; set; }
        public string? TenantId { get; set; }
        public string? ActingUserId { get; set; }

    }

    public class EmiPreviewResponseDto
    {
        public decimal MonthlyEmi { get; set; }
        public decimal TotalInterest { get; set; }
        public decimal TotalPayable { get; set; }
        public List<LoanEmiScheduleDto> Schedule { get; set; } = new();
    }
}
