using System.ComponentModel.DataAnnotations;

namespace Domain.Entities
{
    /// <summary>
    /// Configurable Document Status master (Excel: ED-IFA, FD-IFA, IFC,
    /// ED-Working, FD-Working, As-Built, CLOSED, WNS). Codes are preserved
    /// exactly as used in the Excel; where an abbreviation's business
    /// meaning isn't established anywhere in this application (e.g. "WNS"),
    /// DisplayName is left equal to Code rather than inventing a
    /// definition - update DisplayName later once the real meaning is
    /// confirmed (spec section 26).
    /// </summary>
    public class DocumentStatus : BaseEntity
    {
        [Required]
        [MaxLength(30)]
        public string Code { get; set; } = "";

        [Required]
        [MaxLength(150)]
        public string DisplayName { get; set; } = "";

        public int DisplayOrder { get; set; }

        public override string GetSequencePrefix() => "DST";
    }
}
