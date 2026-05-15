using System.ComponentModel.DataAnnotations;

namespace APP.Models.DTOs
{
    public class EmployeeShiftMappingDto
    {
        public string? Id { get; set; }

        [Required(ErrorMessage = "Employee is required")]
        [Display(Name = "Employee")]
        public string EmployeeId { get; set; } = string.Empty;

        public string? EmployeeName { get; set; }

        [Required(ErrorMessage = "Shift is required")]
        [Display(Name = "Shift")]
        public string ShiftId { get; set; } = string.Empty;

        public string? ShiftName { get; set; }

        [Required(ErrorMessage = "Effective from date is required")]
        [Display(Name = "Effective From")]
        public DateTime EffectiveFrom { get; set; }

        [Display(Name = "Effective To")]
        public DateTime? EffectiveTo { get; set; }

        public bool IsActive { get; set; } = true;
        public string CreatedBy { get; set; }
        public string? ModifiedBy { get; set; }
        public DateTime? CreatedOn { get; set; }
        public DateTime? UpdatedOn { get; set; }
    }
}
