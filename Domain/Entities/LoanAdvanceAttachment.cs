using System.ComponentModel.DataAnnotations;
using static Domain.Enums.EnumExtensions;

namespace Domain.Entities
{
    /// <summary>
    /// Generic supporting-document attachment shared by both
    /// <see cref="EmployeeLoan"/> and <see cref="EmployeeAdvance"/>
    /// requests (ID proof, purpose justification, quotation, medical
    /// bill, etc.) - a single polymorphic table (EntityType + EntityId,
    /// no FK) rather than one attachment table per module, following the
    /// same reasoning already applied to ErrorLog/ApiRequestLog being
    /// tenant-wide shared tables. Storage shape mirrors
    /// <see cref="EmployeeDocument"/> (path + metadata row, not a byte[]
    /// column).
    /// </summary>
    public class LoanAdvanceAttachment : BaseEntity
    {
        public LoanAttachmentEntityType EntityType { get; set; }

        /// <summary>EmployeeLoan.Id or EmployeeAdvance.Id, depending on EntityType. No FK - intentionally polymorphic.</summary>
        [Required]
        public string EntityId { get; set; }

        [Required]
        [MaxLength(255)]
        public string FileName { get; set; }

        [Required]
        [MaxLength(500)]
        public string FilePath { get; set; }

        [Required]
        [MaxLength(100)]
        public string ContentType { get; set; }

        public long FileSizeBytes { get; set; }

        [Required]
        public string UploadedBy { get; set; }
        public virtual User UploadedByUser { get; set; }

        public override string GetSequencePrefix() => "LAA";
    }
}
