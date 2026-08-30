using System.ComponentModel.DataAnnotations;
using static Domain.Enums.EnumExtensions;

namespace Domain.Entities
{
    /// <summary>
    /// Configurable reason master shared by Idle Hours (spec section 11)
    /// and Downtime (spec section 12) - kept as ONE table (rather than two
    /// near-identical masters) because both are just "a reason code for a
    /// non-productive hour," but the Category column keeps them from ever
    /// being mixed together in the UI or reports: the Daily Work Entry
    /// screen only ever offers reasons whose Category matches the selected
    /// activity's WorkCategory (Idle or Downtime).
    /// </summary>
    public class WorkEntryReason : BaseEntity
    {
        public WorkEntryReasonCategory Category { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = "";

        public int DisplayOrder { get; set; }

        public override string GetSequencePrefix() => "WER";
    }
}
