using System.ComponentModel.DataAnnotations;

namespace Domain.Entities
{
    /// <summary>
    /// Minimal client master for the Daily Work Entry module - this
    /// codebase had NO existing Client/Customer entity anywhere (confirmed
    /// by repo-wide search), so this is intentionally new, not a duplicate.
    /// Kept deliberately small (just enough to derive Client from WorkJob,
    /// per the Excel's "Client" column) rather than a full CRM/customer
    /// master - extend only if a real business need shows up.
    /// </summary>
    public class Client : BaseEntity
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = "";

        [MaxLength(50)]
        public string? Code { get; set; }

        public override string GetSequencePrefix() => "CLI";
    }
}
