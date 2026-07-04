using System.ComponentModel.DataAnnotations;
using static APP.Helpers.EnumExtensions;

namespace APP.Models.DTOs
{
    public class LeaveApplicationDto
    {
        public string? Id { get; set; }

        [Display(Name = "Company")]
        public string CompanyId { get; set; }

        [Display(Name = "Branch")]
        public string? BranchId { get; set; }

        [Required(ErrorMessage = "Employee is required")]
        [Display(Name = "Employee")]
        public string EmployeeId { get; set; }

        public string? EmployeeName { get; set; }

        [Required(ErrorMessage = "Leave Type is required")]
        [Display(Name = "Leave Type")]
        public string LeaveTypeId { get; set; }

        public string? LeaveTypeName { get; set; }

        [Required(ErrorMessage = "From Date is required")]
        [DataType(DataType.Date)]
        [Display(Name = "From Date")]
        public DateTime FromDate { get; set; }

        [Required(ErrorMessage = "To Date is required")]
        [DataType(DataType.Date)]
        [Display(Name = "To Date")]
        public DateTime ToDate { get; set; }

        [Range(0.5, 365, ErrorMessage = "Total Days must be greater than 0")]
        [Display(Name = "Total Days")]
        public decimal TotalDays { get; set; }

        [Display(Name = "Half Day")]
        public bool IsHalfDay { get; set; }

        [Display(Name = "Half Day Type")]
        public HalfDayType HalfDayType { get; set; }

        [StringLength(500, ErrorMessage = "Reason cannot exceed 500 characters")]
        public string? Reason { get; set; }

        public ApprovalStatus Status { get; set; }

        public string? ApprovedBy { get; set; }

        public string? ApprovedByName { get; set; }

        public DateTime? ApprovedDate { get; set; }

        [StringLength(500)]
        public string? RejectedReason { get; set; }

        [StringLength(500)]
        public string? DocumentUrl { get; set; }

        public DateTime CreatedOn { get; set; }

        public string? CreatedBy { get; set; }

        public DateTime? ModifiedOn { get; set; }

        public string? ModifiedBy { get; set; }
    }

    public class ApplyLeaveRequestDto
    {
        public string? Id { get; set; }

        [Required(ErrorMessage = "Company is required")]
        public string CompanyId { get; set; }

        [Required(ErrorMessage = "Branch is required")]
        public string BranchId { get; set; }

        [Required(ErrorMessage = "Employee is required")]
        public string EmployeeId { get; set; }

        [Required(ErrorMessage = "Leave Type is required")]
        public string LeaveTypeId { get; set; }

        [Required(ErrorMessage = "From Date is required")]
        [DataType(DataType.Date)]
        public DateTime FromDate { get; set; }

        [Required(ErrorMessage = "To Date is required")]
        [DataType(DataType.Date)]
        public DateTime ToDate { get; set; }

        public int TotalDays { get; set; } = 0;
        public bool IsHalfDay { get; set; }

        public HalfDayType HalfDayType { get; set; }

        [Required(ErrorMessage = "Reason is required")]
        [StringLength(500, ErrorMessage = "Reason cannot exceed 500 characters")]
        public string? Reason { get; set; }

        public IFormFile? UploadDocument { get; set; }

        [StringLength(500)]
        public string? DocumentUrl { get; set; }

        public DateTime CreatedOn { get; set; }

        public string? CreatedBy { get; set; }

        public DateTime? ModifiedOn { get; set; }

        public string? ModifiedBy { get; set; }
        public string TenantId { get; set; }
    }

    public class ApproveLeaveRequestDto
    {
        [Required(ErrorMessage = "Leave Application is required")]
        public string LeaveApplicationId { get; set; }

        [Required(ErrorMessage = "Approver is required")]
        public string ApprovedBy { get; set; }

        [StringLength(500)]
        public string? Remarks { get; set; }

        public DateTime CreatedOn { get; set; }

        public string? CreatedBy { get; set; }
        public string TenantId { get; set; }
    }

    public class RejectLeaveRequestDto
    {
        [Required(ErrorMessage = "Leave Application is required")]
        public string LeaveApplicationId { get; set; }

        [Required(ErrorMessage = "Rejected By is required")]
        public string RejectedBy { get; set; }

        [Required(ErrorMessage = "Rejected Reason is required")]
        [StringLength(500, ErrorMessage = "Rejected Reason cannot exceed 500 characters")]
        public string RejectedReason { get; set; }

        public DateTime RejectedOn { get; set; }
        public string TenantId { get; set; }
    }

    public class CancelLeaveRequestDto
    {
        [Required(ErrorMessage = "Leave Application is required")]
        public string LeaveApplicationId { get; set; }

        [Required(ErrorMessage = "Cancelled By is required")]
        public string CancelledBy { get; set; }

        public DateTime CancelledOn { get; set; }
        public string TenantId { get; set; }
    }

    public class LeaveApplicationFilterRequestDto
    {
        [Display(Name = "Employee")]
        public string? EmployeeId { get; set; }

        [Display(Name = "Leave Type")]
        public string? LeaveTypeId { get; set; }

        [Display(Name = "Status")]
        public ApprovalStatus? Status { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "From Date")]
        public DateTime? FromDate { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "To Date")]
        public DateTime? ToDate { get; set; }
        public string TenantId { get; set; }
    }
}
