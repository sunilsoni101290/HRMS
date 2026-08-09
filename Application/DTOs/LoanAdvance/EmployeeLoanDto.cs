using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.LoanAdvance
{
    /// <summary>
    /// Full read shape for Domain.Entities.EmployeeLoan - used by the
    /// Details/Approval views. For grid listing, prefer the lighter
    /// <see cref="EmployeeLoanListDto"/> instead (fewer joins, no nested
    /// collections) - same "full Dto for Details, slim Dto for Index" split
    /// this codebase already uses (see ProbationConfirmationDto vs
    /// ProbationDueForReviewDto).
    /// </summary>
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

        /// <summary>1=Reducing, 2=Flat.</summary>
        public int InterestMethod { get; set; }
        public string? InterestMethodName { get; set; }

        public string? Purpose { get; set; }

        /// <summary>See Domain.Enums.EnumExtensions.LoanStatus.</summary>
        public int Status { get; set; }
        public string? StatusName { get; set; }

        public int CurrentApprovalLevel { get; set; }
        public int TotalApprovalLevels { get; set; }

        // ---- Maker ----
        public string MakerId { get; set; } = string.Empty;
        public string? MakerName { get; set; }
        public DateTime MakerActionOn { get; set; }
        public string? MakerRemarks { get; set; }

        // ---- Disbursement ----
        public decimal? DisbursedAmount { get; set; }
        public DateTime? DisbursedOn { get; set; }
        public int? DisbursementMode { get; set; }
        public string? DisbursementModeName { get; set; }
        public string? DisbursementReference { get; set; }

        // ---- Balance ----
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

    /// <summary>Slim projection for the Index/DataTables grid - no nested collections.</summary>
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

    /// <summary>
    /// Submitted by the Maker (Employee) to create/submit a loan request -
    /// see EmployeeLoanService.SubmitAsync. See LoanTypeDto's remarks for
    /// the client/server validation split.
    /// </summary>
    public class LoanSubmitDto
    {
        [Required(ErrorMessage = "Employee is required.")]
        public string EmployeeId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Loan Type is required.")]
        public string LoanTypeId { get; set; } = string.Empty;

        [Range(0.01, double.MaxValue, ErrorMessage = "Requested Amount must be greater than zero.")]
        public decimal RequestedAmount { get; set; }

        [Range(1, 360, ErrorMessage = "Tenure Months must be between 1 and 360.")]
        public int TenureMonths { get; set; }

        [StringLength(500)]
        public string? Purpose { get; set; }
        public string? TenantId { get; set; }
        public string? ActingUserId { get; set; }

    }

    /// <summary>Body for the Approve/Reject action at whatever level EmployeeLoan.CurrentApprovalLevel currently is.</summary>
    public class LoanApprovalActionDto
    {
        [Required(ErrorMessage = "EmployeeLoanId is required.")]
        public string EmployeeLoanId { get; set; } = string.Empty;

        /// <summary>1=Approved, 2=Rejected - see Domain.Enums.EnumExtensions.ApprovalDecision.</summary>
        [Range(1, 2, ErrorMessage = "Decision must be 1 (Approved) or 2 (Rejected).")]
        public int Decision { get; set; }

        [StringLength(1000)]
        public string? Remarks { get; set; }

        /// <summary>Only meaningful on the FINAL level's Approve - lets Finance approve a lower amount than requested.</summary>
        [Range(0.01, double.MaxValue, ErrorMessage = "Approved Amount must be greater than zero when specified.")]
        public decimal? ApprovedAmount { get; set; }
        public string? TenantId { get; set; }
        public string? ActingUserId { get; set; }

    }

    /// <summary>Body for Finance marking an Approved loan as Disbursed - triggers EMI schedule generation.</summary>
    public class LoanDisbursementDto
    {
        [Required(ErrorMessage = "EmployeeLoanId is required.")]
        public string EmployeeLoanId { get; set; } = string.Empty;

        [Range(0.01, double.MaxValue, ErrorMessage = "Disbursed Amount must be greater than zero.")]
        public decimal DisbursedAmount { get; set; }

        [Required(ErrorMessage = "Disbursed On date is required.")]
        public DateTime DisbursedOn { get; set; }

        /// <summary>1=BankTransfer, 2=Cheque, 3=PayrollCredit.</summary>
        [Range(1, 3, ErrorMessage = "Disbursement Mode must be 1 (Bank Transfer), 2 (Cheque) or 3 (Payroll Credit).")]
        public int DisbursementMode { get; set; }

        [StringLength(100)]
        public string? DisbursementReference { get; set; }

        /// <summary>First EMI due date - subsequent installments follow monthly from this date.</summary>
        [Required(ErrorMessage = "First EMI Due Date is required.")]
        public DateTime FirstEmiDueDate { get; set; }
        public string? TenantId { get; set; }
        public string? ActingUserId { get; set; }
    }

    /// <summary>Read-only quote returned by ILoanCalculationService.GetPreClosureQuoteAsync before the employee/Finance confirms.</summary>
    public class LoanPreClosureQuoteDto
    {
        public string EmployeeLoanId { get; set; } = string.Empty;
        public DateTime AsOfDate { get; set; }
        public decimal OutstandingPrincipal { get; set; }
        public decimal AccruedInterest { get; set; }
        public decimal PreClosurePenalty { get; set; }
        public decimal TotalPayable { get; set; }
    }

    /// <summary>Body confirming a pre-closure/settlement lump-sum payment.</summary>
    public class LoanSettlementDto
    {
        [Required(ErrorMessage = "EmployeeLoanId is required.")]
        public string EmployeeLoanId { get; set; } = string.Empty;

        [Range(0.01, double.MaxValue, ErrorMessage = "Amount Paid must be greater than zero.")]
        public decimal AmountPaid { get; set; }

        [Required(ErrorMessage = "Payment Date is required.")]
        public DateTime PaymentDate { get; set; }

        [StringLength(100)]
        public string? ReceiptReference { get; set; }

        [StringLength(500)]
        public string? Remarks { get; set; }

        /// <summary>2=PreClosed, 3=SettledOnExit, 4=WrittenOff - see Domain.Enums.EnumExtensions.LoanClosureReason.</summary>
        [Range(2, 4, ErrorMessage = "Closure Reason must be 2 (Pre-Closed), 3 (Settled On Exit) or 4 (Written Off).")]
        public int ClosureReason { get; set; }
        public string? TenantId { get; set; }
        public string? ActingUserId { get; set; }

    }
}
