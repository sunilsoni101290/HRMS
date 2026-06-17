using APP.Attributes;
using System.ComponentModel.DataAnnotations;

namespace APP.Models.DTOs
{
    public class BiometricDeviceDto
    {
        public string? Id { get; set; }

        [Required]
        [Display(Name = "Tenant")]
        public string TenantId { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Company")]
        public string CompanyId { get; set; } = string.Empty;

        [Display(Name = "Branch")]
        public string BranchId { get; set; } = string.Empty;

        [Required(ErrorMessage = "{0} is required.")]
        [StringLength(100, ErrorMessage = "{0} cannot exceed {1} characters.")]
        [Display(Name = "Device Name")]
        public string DeviceName { get; set; } = string.Empty;

        [Required(ErrorMessage = "{0} is required.")]
        [StringLength(50, ErrorMessage = "{0} cannot exceed {1} characters.")]
        [Display(Name = "Device Code")]
        public string DeviceCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "{0} is required.")]
        [Display(Name = "IP Address")]
        [IpAddressValidation]
        public string IPAddress { get; set; } = string.Empty;

        [Required(ErrorMessage = "{0} is required.")]
        [Range(1, 65535, ErrorMessage = "{0} must be between 1 and 65535.")]
        [Display(Name = "Port")]
        public int Port { get; set; }

        //[Required(ErrorMessage = "{0} is required.")]
        //[Url(ErrorMessage = "Invalid API URL.")]
        [Display(Name = "API URL")]
        public string ApiUrl { get; set; } = string.Empty;

        [Required(ErrorMessage = "{0} is required.")]
        [StringLength(100)]
        [Display(Name = "Username")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "{0} is required.")]
        [StringLength(100)]
        [Display(Name = "Password")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Active")]
        public bool IsActive { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public string? ModifiedBy { get; set; }
    }
}
