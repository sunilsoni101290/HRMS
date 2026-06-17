using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Application.DTOs.Masters
{
    public class WeekOffDto
    {
        public string? Id { get; set; }

        [Required(ErrorMessage = "Week Off Day is required")]
        [Display(Name = "Week Off Day")]
        public DayOfWeek Day { get; set; }
        public string? TenantId { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public string? ModifiedBy { get; set; }
    }
}
