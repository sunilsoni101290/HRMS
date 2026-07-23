using System.ComponentModel.DataAnnotations;
using static APP.Helpers.EnumExtensions;

namespace APP.Models.DTOs
{
    public class AttendanceRegularizationDto
    {
        public string? Id { get; set; }

        [Required(ErrorMessage = "Employee is required")]
        [Display(Name = "Employee")]
        public string EmployeeId { get; set; }

        public string? EmployeeName { get; set; }

        [Required(ErrorMessage = "Date is required")]
        [DataType(DataType.Date)]
        [Display(Name = "Attendance Date")]
        public DateTime Date { get; set; }

        public string? AttendanceId { get; set; }

        [Display(Name = "Original First In")]
        public DateTime? OriginalFirstIn { get; set; }

        [Display(Name = "Original Last Out")]
        public DateTime? OriginalLastOut { get; set; }

        [Display(Name = "Requested First In")]
        public DateTime? RequestedFirstIn { get; set; }

        [Display(Name = "Requested Last Out")]
        public DateTime? RequestedLastOut { get; set; }

        [StringLength(500, ErrorMessage = "Reason cannot exceed 500 characters")]
        public string? Reason { get; set; }

        public ApprovalStatus Status { get; set; }

        public string? ApprovedBy { get; set; }

        public string? ApprovedByName { get; set; }

        public DateTime? ApprovedDate { get; set; }

        [StringLength(500)]
        public string? RejectedReason { get; set; }

        // Multi-level approval chain (1 = Reporting Manager, 2 = Department
        // Head, 3 = HR) - only meaningful while Status == Pending.
        public int CurrentLevel { get; set; }

        public string? CurrentLevelName { get; set; }

        public string? CurrentApproverEmployeeId { get; set; }

        [StringLength(500)]
        public string? SendBackReason { get; set; }

        public DateTime CreatedOn { get; set; }

        public string? CreatedBy { get; set; }

        public DateTime? ModifiedOn { get; set; }

        public string? ModifiedBy { get; set; }
    }

    public class RequestRegularizationDto
    {
        public string? Id { get; set; }

        [Required(ErrorMessage = "Employee is required")]
        public string EmployeeId { get; set; }

        [Required(ErrorMessage = "Date is required")]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; }

        [Display(Name = "Requested First In")]
        public DateTime? RequestedFirstIn { get; set; }

        [Display(Name = "Requested Last Out")]
        public DateTime? RequestedLastOut { get; set; }

        [Required(ErrorMessage = "Reason is required")]
        [StringLength(500, ErrorMessage = "Reason cannot exceed 500 characters")]
        public string? Reason { get; set; }

        public DateTime CreatedOn { get; set; }

        public string? CreatedBy { get; set; }

        public DateTime? ModifiedOn { get; set; }

        public string? ModifiedBy { get; set; }

        public string TenantId { get; set; }
    }

    public class ApproveRegularizationRequestDto
    {
        [Required(ErrorMessage = "Attendance Correction is required")]
        public string AttendanceRegularizationId { get; set; }

        [Required(ErrorMessage = "Approver is required")]
        public string ApprovedBy { get; set; }

        [StringLength(500)]
        public string? Remarks { get; set; }

        public DateTime CreatedOn { get; set; }

        public string? CreatedBy { get; set; }
        public string TenantId { get; set; }
    }

    public class RejectRegularizationRequestDto
    {
        [Required(ErrorMessage = "Attendance Correction is required")]
        public string AttendanceRegularizationId { get; set; }

        [Required(ErrorMessage = "Rejected By is required")]
        public string RejectedBy { get; set; }

        [Required(ErrorMessage = "Rejected Reason is required")]
        [StringLength(500, ErrorMessage = "Rejected Reason cannot exceed 500 characters")]
        public string RejectedReason { get; set; }

        public DateTime RejectedOn { get; set; }
        public string TenantId { get; set; }
    }

    public class SendBackRegularizationRequestDto
    {
        [Required(ErrorMessage = "Attendance Correction is required")]
        public string AttendanceRegularizationId { get; set; }

        [Required(ErrorMessage = "Acting user is required")]
        public string ActionBy { get; set; }

        [Required(ErrorMessage = "Please explain why this is being sent back.")]
        [StringLength(500, ErrorMessage = "Reason cannot exceed 500 characters")]
        public string SendBackReason { get; set; }
    }

    public class CancelRegularizationRequestDto
    {
        [Required(ErrorMessage = "Attendance Correction is required")]
        public string AttendanceRegularizationId { get; set; }

        [Required(ErrorMessage = "Cancelled By is required")]
        public string CancelledBy { get; set; }

        public DateTime CancelledOn { get; set; }
        public string TenantId { get; set; }
    }

    public class AttendanceRegularizationFilterRequestDto
    {
        [Display(Name = "Employee")]
        public string? EmployeeId { get; set; }

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
