using System;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Leaves
{
    public class ApprovalDelegationDto
    {
        public string Id { get; set; }

        public string DelegatorEmployeeId { get; set; }
        public string? DelegatorEmployeeName { get; set; }

        public string DelegateEmployeeId { get; set; }
        public string? DelegateEmployeeName { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public string? Reason { get; set; }

        public bool IsActive { get; set; }

        // Convenience flag for the UI - IsActive means "not manually
        // revoked", this means "currently in its date window" too. A
        // delegation can be IsActive=true but not currently in effect
        // (future-dated or already past its EndDate).
        public bool IsCurrentlyInEffect { get; set; }

        public DateTime CreatedOn { get; set; }
        public string? CreatedBy { get; set; }
    }

    public class CreateApprovalDelegationRequestDto
    {
        // Never trusted from the client for a self-service caller - the
        // controller always overwrites this with the caller's own
        // EmployeeId, exactly like LeaveApplicationController.Create does
        // for ApplyLeaveRequestDto.EmployeeId.
        [Required(ErrorMessage = "Delegator is required")]
        public string DelegatorEmployeeId { get; set; }

        [Required(ErrorMessage = "Delegate is required")]
        public string DelegateEmployeeId { get; set; }

        [Required(ErrorMessage = "Start date is required")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "End date is required")]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        [StringLength(500, ErrorMessage = "Reason cannot exceed 500 characters")]
        public string? Reason { get; set; }

        public string? TenantId { get; set; }
        public string? CreatedBy { get; set; }
    }
}
