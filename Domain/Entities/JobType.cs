using System.ComponentModel.DataAnnotations;

namespace Domain.Entities
{
    /// <summary>
    /// Configurable Job Type master (Excel "Job type and work done" sheet -
    /// Structural Steel, Equipment, Piping, Skids Packages, Project
    /// Management, Estimation Work, FEA, PE Services, Others). Deliberately
    /// a master table, never hard-coded in controllers/views (spec section
    /// 4.4).
    /// </summary>
    public class JobType : BaseEntity
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = "";

        [MaxLength(30)]
        public string? Code { get; set; }

        public int DisplayOrder { get; set; }

        /// <summary>Skids Packages activities are filtered by SkidsDiscipline (see WorkActivity) - all other Job Types leave this false.</summary>
        public bool HasDisciplines { get; set; }

        public override string GetSequencePrefix() => "JBT";
    }
}
