using System.ComponentModel.DataAnnotations;
using static APP.Helpers.EnumExtensions;

namespace APP.Models.DTOs
{
    public class LeaveBalanceDto
    {
        public string? Id { get; set; }

        [Required(ErrorMessage = "Employee is required")]
        public string EmployeeId { get; set; }

        public string? EmployeeName { get; set; }

        [Required(ErrorMessage = "Leave Type is required")]
        public string LeaveTypeId { get; set; }

        public string? LeaveTypeName { get; set; }

        [Required(ErrorMessage = "Year is required")]
        public int Year { get; set; }

        // Previous Year Balance
        public decimal OpeningBalance { get; set; }

        // Annual Allocation
        public decimal Allocated { get; set; }

        // Extra Leave Given
        public decimal Credited { get; set; }

        // Leave Taken
        public decimal Used { get; set; }

        // Previous Year Transfer
        public decimal CarryForward { get; set; }

        // Final Available Balance
        public decimal Balance { get; set; }

        public string? CreatedBy { get; set; }

        public string? TenantId { get; set; }

        public DateTime? ModifiedOn { get; set; }

        public string? ModifiedBy { get; set; }
    }
    public class LeaveBalanceTransactionDto
    {
        public string? Id { get; set; }

        [Required]
        public string EmployeeId { get; set; }

        public string? EmployeeName { get; set; }

        [Required]
        public string LeaveTypeId { get; set; }

        public string? LeaveTypeName { get; set; }

        [Required]
        public int Year { get; set; }

        [Required]
        [Display(Name = "Transaction Type")]
        public LeaveTransactionType TransactionType { get; set; }

        [Display(Name = "Quantity")]
        public decimal Quantity { get; set; }

        [Display(Name = "Balance Before")]
        public decimal BalanceBefore { get; set; }

        [Display(Name = "Balance After")]
        public decimal BalanceAfter { get; set; }

        [MaxLength(500)]
        [Display(Name = "Remarks")]
        public string? Remarks { get; set; }

        [Display(Name = "Transaction Date")]
        public DateTime TransactionDate { get; set; }

        public string CreatedBy { get; set; }

        public DateTime? ModifiedOn { get; set; }

        public string? ModifiedBy { get; set; }
    }

    public class AllocateLeaveRequestDto
    {
        [Required(ErrorMessage = "Employee is required.")]
        [Display(Name = "Employee")]
        public string EmployeeId { get; set; }

        [Required(ErrorMessage = "Year is required.")]
        [Range(2000, 9999, ErrorMessage = "Please enter a valid year.")]
        [Display(Name = "Year")]
        public int Year { get; set; }
    }

    public class LeaveAdjustmentRequestDto
    {
        [Required(ErrorMessage = "Employee is required.")]
        [Display(Name = "Employee")]
        public string EmployeeId { get; set; }

        [Required(ErrorMessage = "Leave Type is required.")]
        [Display(Name = "Leave Type")]
        public string LeaveTypeId { get; set; }

        [Required(ErrorMessage = "Days is required.")]
        [Range(0.01, 365, ErrorMessage = "Days must be greater than zero.")]
        [Display(Name = "Number of Days")]
        public decimal Days { get; set; }
    }

    public class CarryForwardLeaveRequestDto : IValidatableObject
    {
        [Required(ErrorMessage = "Employee is required.")]
        [Display(Name = "Employee")]
        public string EmployeeId { get; set; }

        [Required(ErrorMessage = "From Year is required.")]
        [Range(2000, 9999, ErrorMessage = "Please enter a valid year.")]
        [Display(Name = "From Year")]
        public int FromYear { get; set; }

        [Required(ErrorMessage = "To Year is required.")]
        [Range(2000, 9999, ErrorMessage = "Please enter a valid year.")]
        [Display(Name = "To Year")]
        public int ToYear { get; set; }

        public IEnumerable<ValidationResult> Validate(
            ValidationContext validationContext)
        {
            if (ToYear <= FromYear)
            {
                yield return new ValidationResult(
                    "To Year must be greater than From Year.",
                    new[] { nameof(ToYear) });
            }
        }
    }

    public class LeaveTransactionFilterRequestDto
    {
        [Display(Name = "Employee")]
        public string? EmployeeId { get; set; }

        [Display(Name = "Leave Type")]
        public string? LeaveTypeId { get; set; }

        [Range(2000, 9999, ErrorMessage = "Please enter a valid year.")]
        [Display(Name = "Year")]
        public int? Year { get; set; }
    }

    public class EmployeeTransactionRequestDto
    {
        [Required(ErrorMessage = "Employee is required.")]
        [Display(Name = "Employee")]
        public string EmployeeId { get; set; } = string.Empty;

        [Display(Name = "Leave Type")]
        public string? LeaveTypeId { get; set; } = string.Empty;
        public int? Year { get; set; }
    }

    public class TransactionDateRangeRequestDto : IValidatableObject
    {
        [Required(ErrorMessage = "From Date is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "From Date")]
        public DateTime? FromDate { get; set; }

        [Required(ErrorMessage = "To Date is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "To Date")]
        public DateTime? ToDate { get; set; }

        public IEnumerable<ValidationResult> Validate(
            ValidationContext validationContext)
        {
            if (FromDate.HasValue &&
                ToDate.HasValue &&
                ToDate.Value.Date < FromDate.Value.Date)
            {
                yield return new ValidationResult(
                    "To Date must be greater than or equal to From Date.",
                    new[] { nameof(ToDate) });
            }
        }
    }
}
