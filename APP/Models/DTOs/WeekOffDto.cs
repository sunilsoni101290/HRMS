using System.ComponentModel.DataAnnotations;
using static Domain.Enums.EnumExtensions;

namespace APP.Models.DTOs
{
    public class WeekOffDto
    {
        public string? Id { get; set; }

        [Required(ErrorMessage = "Week Off Day is required")]
        [Display(Name = "Week Off Day")]
        public DayOfWeek Day { get; set; }

        // Additive - defaults to EveryWeek (the original, fixed-weekday
        // behavior) so existing configurations are unaffected.
        [Display(Name = "Pattern")]
        public WeekOffPatternType PatternType { get; set; } = WeekOffPatternType.EveryWeek;

        // Only meaningful when PatternType == NthWeekdayOfMonth: selected
        // occurrence numbers (1-5), e.g. [2, 4] for "2nd and 4th".
        [Display(Name = "Occurrences")]
        public List<int>? Occurrences { get; set; }

        public string? TenantId { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public string? ModifiedBy { get; set; }
    }
}
