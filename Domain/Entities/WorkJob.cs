using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static Domain.Enums.EnumExtensions;

namespace Domain.Entities
{
    /// <summary>
    /// Job/Project master for the Daily Work Entry module (Excel's "Job
    /// Number"/"Job Name"). Named WorkJob rather than bare "Job" to avoid
    /// any confusion with the unrelated recruitment JobOpening entity - no
    /// equivalent Job/Project master existed anywhere in this codebase
    /// before this module.
    /// </summary>
    public class WorkJob : BaseEntity
    {
        [Required]
        [MaxLength(50)]
        public string JobNumber { get; set; } = "";

        [Required]
        [MaxLength(200)]
        public string JobName { get; set; } = "";

        [Required]
        public string ClientId { get; set; } = "";

        [ForeignKey(nameof(ClientId))]
        public virtual Client? Client { get; set; }

        public WorkJobStatus Status { get; set; } = WorkJobStatus.Active;

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public override string GetSequencePrefix() => "WJB";
    }
}
