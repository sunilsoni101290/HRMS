using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using static Domain.Enums.EnumExtensions;

namespace Application.DTOs.Leaves
{
    public class LeaveApplicationDto
    {
        public string? Id { get; set; }

        [Required(ErrorMessage = "Company is required")]
        [Display(Name = "Company")]
        public string CompanyId { get; set; }

        [Required(ErrorMessage = "Branch is required")]
        [Display(Name = "Branch")]
        public string BranchId { get; set; }

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

        // Multi-level approval chain (1 = Reporting Manager, 2 = Department
        // Head, 3 = HR) - only meaningful while Status == Pending.
        public int CurrentLevel { get; set; }

        public string? CurrentLevelName { get; set; }

        // The specific employee who can currently act, when the level maps
        // to one (Level 1/2). Null for Level 3, which is role-based (any HR
        // user) rather than tied to one person.
        public string? CurrentApproverEmployeeId { get; set; }

        [StringLength(500)]
        public string? SendBackReason { get; set; }

        [StringLength(500)]
        public string? DocumentUrl { get; set; }

        public DateTime CreatedOn { get; set; }

        public string? CreatedBy { get; set; }

        public DateTime? ModifiedOn { get; set; }

        public string? ModifiedBy { get; set; }
    }

    public class ApplyLeaveRequestDto
    {
        public string? TenantId { get; set; }

        public string? CompanyId { get; set; }

        public string? BranchId { get; set; }

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

        public bool IsHalfDay { get; set; }

        public HalfDayType HalfDayType { get; set; }

        [Required(ErrorMessage = "Reason is required")]
        [StringLength(500, ErrorMessage = "Reason cannot exceed 500 characters")]
        public string? Reason { get; set; }

        [StringLength(500)]
        public string? DocumentUrl { get; set; }

        public DateTime CreatedOn { get; set; }

        public string? CreatedBy { get; set; }

        public DateTime? ModifiedOn { get; set; }

        public string? ModifiedBy { get; set; }
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
    }

    public class SendBackLeaveRequestDto
    {
        [Required(ErrorMessage = "Leave Application is required")]
        public string LeaveApplicationId { get; set; }

        [Required(ErrorMessage = "Sent By is required")]
        public string SentBackBy { get; set; }

        [Required(ErrorMessage = "Please explain why this is being sent back.")]
        [StringLength(500, ErrorMessage = "Reason cannot exceed 500 characters")]
        public string Reason { get; set; }
    }

    public class CancelLeaveRequestDto
    {
        [Required(ErrorMessage = "Leave Application is required")]
        public string LeaveApplicationId { get; set; }

        [Required(ErrorMessage = "Cancelled By is required")]
        public string CancelledBy { get; set; }

        public DateTime CancelledOn { get; set; }
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
    }

}
