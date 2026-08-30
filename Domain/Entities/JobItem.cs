using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static Domain.Enums.EnumExtensions;

namespace Domain.Entities
{
    /// <summary>
    /// The specific Structure/Equipment/Job item under a WorkJob (Excel's
    /// "STR/EQPT/JOB ID" column, e.g. Job PT001 -> Structures PB-28/PB-05/
    /// PB-30). TotalWeightMT is only ever populated/shown for structural
    /// items - never forced on Job Types where a weight doesn't apply
    /// (spec section 25).
    /// </summary>
    public class JobItem : BaseEntity
    {
        [Required]
        public string WorkJobId { get; set; } = "";

        [ForeignKey(nameof(WorkJobId))]
        public virtual WorkJob? WorkJob { get; set; }

        [Required]
        [MaxLength(100)]
        public string Code { get; set; } = "";

        [MaxLength(300)]
        public string? Description { get; set; }

        public JobItemType ItemType { get; set; } = JobItemType.Other;

        /// <summary>Structure weight in metric tons - used only by the "Engineering Hours / MT" calculation for structural jobs (spec section 25). Null everywhere else.</summary>
        public decimal? TotalWeightMT { get; set; }

        /// <summary>Current document status for this structure/equipment (Excel: ED-IFA/FD-IFA/IFC/etc., shown per JobItem row in the Monthly Employee Work Report and the Engineering report - spec section 26). Nullable - not every JobItem tracks a document deliverable.</summary>
        public string? DocumentStatusId { get; set; }

        [ForeignKey(nameof(DocumentStatusId))]
        public virtual DocumentStatus? DocumentStatus { get; set; }

        public override string GetSequencePrefix() => "JIT";
    }
}
