using System.ComponentModel.DataAnnotations;

namespace APP.Models.DTOs
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
        public bool IsCurrentlyInEffect { get; set; }

        public DateTime CreatedOn { get; set; }
        public string? CreatedBy { get; set; }
    }

    public class CreateApprovalDelegationRequestDto
    {
        // Overwritten server-side with the caller's own EmployeeId - never
        // trusted from the posted form, same convention as
        // LeaveApplicationController.Create's EmployeeId handling.
        public string DelegatorEmployeeId { get; set; }

        [Required(ErrorMessage = "Delegate is required")]
        [Display(Name = "Delegate")]
        public string DelegateEmployeeId { get; set; }

        [Required(ErrorMessage = "Start date is required")]
        [DataType(DataType.Date)]
        [Display(Name = "Start Date")]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "End date is required")]
        [DataType(DataType.Date)]
        [Display(Name = "End Date")]
        public DateTime EndDate { get; set; }

        [StringLength(500, ErrorMessage = "Reason cannot exceed 500 characters")]
        public string? Reason { get; set; }

        public string? TenantId { get; set; }
        public string? CreatedBy { get; set; }
    }
}
