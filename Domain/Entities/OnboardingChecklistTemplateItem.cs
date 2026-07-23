using static Domain.Enums.EnumExtensions;

namespace Domain.Entities
{
    // HR-managed master/template checklist list, per tenant. New
    // OnboardingCases are seeded with a snapshot copy of the currently
    // active (IsActive == true, inherited from BaseEntity) rows into
    // OnboardingChecklistItem - editing a template afterwards never changes
    // already-created cases.
    public class OnboardingChecklistTemplateItem : BaseEntity
    {
        public OnboardingStageType StageType { get; set; }

        public string Title { get; set; }
        public string? Description { get; set; }

        public bool IsMandatory { get; set; }
        public int SortOrder { get; set; }

        public override string GetSequencePrefix() => "ONT";
    }
}
