using System.ComponentModel.DataAnnotations;

namespace Domain.Entities
{
    public class FaqItem : BaseEntity
    {
        // Null = visible to every company under the tenant; set to scope a
        // question to one company only.
        public string? CompanyId { get; set; }

        [Required, MaxLength(100)]
        public string Category { get; set; }

        [Required, MaxLength(300)]
        public string Question { get; set; }

        [Required]
        public string Answer { get; set; }

        public int DisplayOrder { get; set; } = 0;

        // Reuses BaseEntity.IsActive as the publish/unpublish toggle -
        // inactive FAQs are kept (for admin editing) but hidden from the
        // employee-facing Browse FAQ page.

        public override string GetSequencePrefix() => "FAQ";
    }
}
