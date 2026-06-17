using System.ComponentModel.DataAnnotations;
using static APP.Helpers.EnumExtensions;

namespace APP.Models.DTOs
{
    public class BiometricAttendanceLogDto
    {
        public string? Id { get; set; }

        [Required(ErrorMessage = "{0} is required.")]
        [Display(Name = "Device")]
        public string DeviceId { get; set; } = string.Empty;

        [Required(ErrorMessage = "{0} is required.")]
        [Display(Name = "Biometric Employee Code")]
        [StringLength(50, ErrorMessage = "{0} cannot exceed {1} characters.")]
        public string BiometricEmployeeCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "{0} is required.")]
        [Display(Name = "Punch Time")]
        public DateTime PunchTime { get; set; }

        [Required(ErrorMessage = "{0} is required.")]
        [Display(Name = "Punch Type")]
        public PunchType PunchType { get; set; }

        [Display(Name = "Processed")]
        public bool IsProcessed { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public string? ModifiedBy { get; set; }
    }


    public class EmployeeBiometricMappingDto
    {
        public string? Id { get; set; }

        [Required(ErrorMessage = "{0} is required.")]
        [Display(Name = "Employee")]
        public string EmployeeId { get; set; } = string.Empty;

        [Required(ErrorMessage = "{0} is required.")]
        [Display(Name = "Biometric Employee Code")]
        public string BiometricEmployeeCode { get; set; } = string.Empty;

        [Display(Name = "Card Number")]
        [StringLength(50, ErrorMessage = "{0} cannot exceed {1} characters.")]
        public string? CardNumber { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public string? ModifiedBy { get; set; }
    }
}
