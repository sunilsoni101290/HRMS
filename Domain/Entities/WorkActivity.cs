using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static Domain.Enums.EnumExtensions;

namespace Domain.Entities
{
    /// <summary>
    /// Configurable Work Activity master (spec section 5) - never
    /// hard-coded in controllers/views. The Daily Work Entry activity
    /// dropdown filters by JobTypeId (and SkidsDiscipline when the selected
    /// Job Type is Skids Packages). WorkCategory drives Direct/Indirect/
    /// Idle/Downtime reporting.
    /// </summary>
    public class WorkActivity : BaseEntity
    {
        [Required]
        public string JobTypeId { get; set; } = "";

        [ForeignKey(nameof(JobTypeId))]
        public virtual JobType? JobType { get; set; }

        /// <summary>Only set when JobType = Skids Packages (Excel: Piping/Equipment/Structural/E&I sub-lists). Null for every other Job Type.</summary>
        public SkidsDiscipline? SkidsDiscipline { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = "";

        public WorkCategory WorkCategory { get; set; } = WorkCategory.Direct;

        public int DisplayOrder { get; set; }

        /// <summary>True only for the "Downtime" activity row - tells the UI to require a WorkEntryReason (Category=Downtime) when this activity is selected.</summary>
        public bool RequiresReason { get; set; }

        /// <summary>True for "Others.. Please specify" rows - tells the UI to show a free-text box that is saved into DailyWorkEntry.Remarks.</summary>
        public bool AllowFreeTextOther { get; set; }

        public override string GetSequencePrefix() => "WAC";
    }
}
