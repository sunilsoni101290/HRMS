using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using static Domain.Enums.EnumExtensions;

namespace Application.DTOs.Masters
{
    public class WeekOffDto
    {
        public string? Id { get; set; }

        [Required(ErrorMessage = "Week Off Day is required")]
        [Display(Name = "Week Off Day")]
        public DayOfWeek Day { get; set; }

        // Additive. Defaults to EveryWeek so every caller that never sends
        // this field (e.g. an older client) keeps getting the original
        // "this weekday every week" behavior.
        [Display(Name = "Pattern")]
        public WeekOffPatternType PatternType { get; set; } = WeekOffPatternType.EveryWeek;

        // Only meaningful when PatternType == NthWeekdayOfMonth: the
        // selected occurrence numbers (1-5), e.g. [2, 4] for "2nd and 4th".
        // Empty/null for EveryWeek.
        [Display(Name = "Occurrences")]
        public List<int>? Occurrences { get; set; }

        public string? TenantId { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public string? ModifiedBy { get; set; }
    }
}
