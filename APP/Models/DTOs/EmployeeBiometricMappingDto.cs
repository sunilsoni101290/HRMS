using System.ComponentModel.DataAnnotations;

namespace APP.Models.DTOs
{
    public class EmployeeBiometricMappingDto
    {
        public string? Id { get; set; }

        [Required(ErrorMessage = "{0} is required.")]
        [Display(Name = "Employee")]
        public string EmployeeId { get; set; } = string.Empty;

        // Only used to show the employee's name in the list - not sent back to the API.
        public string? EmployeeName { get; set; }

        [Required(ErrorMessage = "{0} is required.")]
        [StringLength(50)]
        [Display(Name = "Biometric Employee Code")]
        public string BiometricEmployeeCode { get; set; } = string.Empty;

        [Display(Name = "Card Number")]
        public string? CardNumber { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;
    }
}
