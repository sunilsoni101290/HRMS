using System;
using static Domain.Enums.EnumExtensions;

namespace Domain.Entities
{
    // A single checklist row under an OnboardingCase, snapshot-copied from
    // active OnboardingChecklistTemplateItem rows (or seeded from inline
    // defaults) at case-creation time - later template edits deliberately do
    // NOT retroactively change already-created cases.
    public class OnboardingChecklistItem : BaseEntity
    {
        public string OnboardingCaseId { get; set; }
        public virtual OnboardingCase OnboardingCase { get; set; }

        public OnboardingStageType StageType { get; set; }

        public string Title { get; set; }
        public string? Description { get; set; }

        public bool IsMandatory { get; set; }
        public int SortOrder { get; set; }

        public OnboardingChecklistItemStatus Status { get; set; }
        public DateTime? CompletedOn { get; set; }
        public string? CompletedBy { get; set; }

        public string? Remarks { get; set; }

        public override string GetSequencePrefix() => "ONC";
    }
}
