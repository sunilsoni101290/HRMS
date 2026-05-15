using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Application.DTOs.Attendances
{
    public class ShiftDto
    {
        public string? Id { get; set; }

        [Required(ErrorMessage = "Shift name is required")]
        [MaxLength(100)]
        [Display(Name = "Shift Name")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Start time is required")]
        [Display(Name = "Start Time")]
        public TimeSpan StartTime { get; set; }

        [Required(ErrorMessage = "End time is required")]
        [Display(Name = "End Time")]
        public required TimeSpan EndTime { get; set; }

        [Display(Name = "Grace In Minutes")]
        public int GraceInMinutes { get; set; } = 0;

        [Display(Name = "Grace Out Minutes")]
        public int GraceOutMinutes { get; set; } = 0;

        [Required(ErrorMessage = "Half day minutes is required")]
        [Display(Name = "Half Day Minutes")]
        public int HalfDayMinutes { get; set; }

        [Required(ErrorMessage = "Full day minutes is required")]
        [Display(Name = "Full Day Minutes")]
        public int FullDayMinutes { get; set; }

        [Display(Name = "Night Shift")]
        public bool IsNightShift { get; set; } = false;

        [Required(ErrorMessage = "Minimum working minutes is required")]
        [Display(Name = "Minimum Working Minutes")]
        public int MinimumWorkingMinutes { get; set; }

        [Required(ErrorMessage = "Maximum working minutes is required")]
        [Display(Name = "Maximum Working Minutes")]
        public int MaximumWorkingMinutes { get; set; }

        public string? TenantId { get; set; }
        public string? TenantName { get; set; }

        [Required(ErrorMessage = "Created By is required")] 
        public string CreatedBy { get; set; }

        public DateTime? CreatedOn { get; set; }
        public DateTime? UpdatedOn { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
