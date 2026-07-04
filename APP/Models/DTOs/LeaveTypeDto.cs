using System.ComponentModel.DataAnnotations;

namespace APP.Models.DTOs
{
    public class LeaveTypeDto
    {
        public string? Id { get; set; }

        [Required(ErrorMessage = "Leave Type Name is required")]
        [MaxLength(100)]
        public string Name { get; set; }

        [Required(ErrorMessage = "Maximum Days Per Year is required")]
        [Display(Name = "Max Days Per Year")]
        public int MaxDaysPerYear { get; set; }

        [Display(Name = "Paid Leave")]
        public bool IsPaid { get; set; } = true;

        [Display(Name = "Allow Carry Forward")]
        public bool AllowCarryForward { get; set; }

        [Display(Name = "Maximum Carry Forward Days")]
        public int? MaxCarryForwardDays { get; set; }

        [Display(Name = "Allow Half Day")]
        public bool AllowHalfDay { get; set; }

        public string? TenantId { get; set; }

        public string CreatedBy { get; set; }

        public DateTime? ModifiedOn { get; set; }

        public string? ModifiedBy { get; set; }
    }
}
