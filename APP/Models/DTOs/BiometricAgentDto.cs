using System.ComponentModel.DataAnnotations;

namespace APP.Models.DTOs
{
    public class BiometricAgentDto
    {
        public string? Id { get; set; }

        [Required]
        public string TenantId { get; set; } = string.Empty;

        [Required(ErrorMessage = "{0} is required.")]
        [StringLength(50, ErrorMessage = "{0} cannot exceed {1} characters.")]
        [Display(Name = "Agent Code")]
        public string AgentCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "{0} is required.")]
        [StringLength(100, ErrorMessage = "{0} cannot exceed {1} characters.")]
        [Display(Name = "Agent Name")]
        public string AgentName { get; set; } = string.Empty;

        /// <summary>Only meaningful right after Create - see the Details page for the confirmation banner.</summary>
        [Display(Name = "Agent Key")]
        public string? AgentKey { get; set; }

        [Display(Name = "Machine Name")]
        public string? MachineName { get; set; }

        [Display(Name = "Agent Version")]
        public string? AgentVersion { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Last Heartbeat")]
        public DateTime? LastHeartbeat { get; set; }

        [Display(Name = "Online")]
        public bool IsOnline { get; set; }

        [Display(Name = "Devices")]
        public int DeviceCount { get; set; }

        public string CreatedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public string? ModifiedBy { get; set; }
    }
}
